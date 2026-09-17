using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using UserService.Repositories;
using UserService.Services;
using DbUp;

var builder = WebApplication.CreateBuilder(args);

// Application Insights — enabled when APPLICATIONINSIGHTS_CONNECTION_STRING is set in the environment
var appInsightsConnString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnString))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Problem Details for RFC 7807 standardized error responses
builder.Services.AddProblemDetails();

// hard: add memoryCache to prevent DOS during per request token blacklist checks
builder.Services.AddMemoryCache();
// Configure restrictive CORS policy for client authentication
//hard: Allow Vite port (5173) and React port (3000) by default to prevent CORS preflight blocks during dev
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:5173", "http://127.0.0.1:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("ArenaClientCors", policy =>
    {
        // hard: mitigated origin spoofing. replace critical wildcardSetIsOriginAllowed(origin => true) with explicit origin whitelist
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// hard: register ip based rate limiting to prevent brute force credential attacks, account spamming and DOS
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // strinct fixed window policy for authentication endpoints(login,signup,password reset)
    options.AddPolicy("AuthIpLimiter",httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                //hard: 100 permits in Development for test runs; 5 in Production
                PermitLimit = builder.Environment.IsDevelopment() ? 100 : 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Standard policy for general authenticated profile mutations
    options.AddPolicy("GeneralApiLimiter", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 2
            }));
});
// Dapper configuration
DefaultTypeMap.MatchNamesWithUnderscores = true;

// Repositories and Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenBlacklistRepository, TokenBlacklistRepository>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
builder.Services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserProfileService>();

// // hard: migrated from symmetric hmac to rs256 asymmetric cryptography to prevent microservice key confusion attacks
// var pub
// JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]
    ?? "Arena.UserService";
var jwtAudience = builder.Configuration["JwtSettings:Audience"]
    ?? "Arena.Platform";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            //hard: let ASP.NET read token from secure cookie instead of Authorization: Bearer header
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("arena_access_token", out var token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var blacklistService = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                var jti = context.Principal?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value
                    ?? context.Principal?.Claims.FirstOrDefault(c => c.Type == "jti")?.Value
                    ?? context.Principal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.SerialNumber)?.Value;

                if (!string.IsNullOrEmpty(jti) && await blacklistService.IsTokenRevokedAsync(jti))
                {
                    context.Fail("Token has been revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Run DbUp migrations against the User database before accepting requests
var connectionString = builder.Configuration.GetConnectionString("UserDb")
    ?? throw new InvalidOperationException("UserDb connection string is not configured.");

int maxRetries = 10;
int delaySeconds = 2;
bool migrationSucceeded = false;

for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        var upgrader = DeployChanges.To
            .MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(System.Reflection.Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (result.Successful)
        {
            migrationSucceeded = true;
            break;
        }
        app.Logger.LogWarning("UserService migration attempt {Attempt}/{MaxRetries} failed: {Error}", attempt, maxRetries, result.Error);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("UserService migration attempt {Attempt}/{MaxRetries} threw exception: {Message}", attempt, maxRetries, ex.Message);
    }

    if (attempt < maxRetries)
    {
        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
    }
}

if (!migrationSucceeded)
{
    throw new InvalidOperationException("Failed to apply UserService database migrations after maximum retry attempts.");
}

// Exception Handling at the very top of the HTTP pipeline
app.UseExceptionHandler();

//hard: Injected defensive security headers and Content-Security-Policy to block clickjacking and cross-site framing
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none';");
    await next();
});


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
//hard: Rate limiter pipeline placed prior to routing to drop abusive floods at the ingress boundary
app.UseRateLimiter();
// CORS must run before Authentication and Authorization
app.UseCors("ArenaClientCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();