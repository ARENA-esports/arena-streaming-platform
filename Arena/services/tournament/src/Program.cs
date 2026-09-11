using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DbUp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TournamentService.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register Problem Details for RFC 7807 standardized error responses
builder.Services.AddProblemDetails();

// Restrictive CORS policy for Arena web clients
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("ArenaClientCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure OpenAPI / Swagger documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Arena Tournament Service API",
        Version = "v1",
        Description = "Microservice managing tournaments, season calendars, and lifecycle status."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT Bearer token: Bearer {your_jwt_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

/*
    JWT Configuration supporting RS256 (asymmetric RSA) and HS256 (symmetric).
*/
var jwtSecret = builder.Configuration["JwtSettings:Secret"];
var rsaPublicKeyPem = builder.Configuration["JwtSettings:RsaPublicKey"]
    ?? builder.Configuration["JwtSettings:PublicKey"];

var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "Arena.UserService";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "Arena.Platform";

var signingKeys = new List<SecurityKey>();

if (!string.IsNullOrEmpty(jwtSecret))
{
    signingKeys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)));
}

if (!string.IsNullOrEmpty(rsaPublicKeyPem))
{
    var rsa = RSA.Create();
    rsa.ImportFromPem(rsaPublicKeyPem);
    signingKeys.Add(new RsaSecurityKey(rsa));
}

if (signingKeys.Count == 0)
{
    const string fallbackSecret = "Arena_Secret_Key_For_Jwt_Token_Signing_2026_SE3022_Production_Grade!";
    signingKeys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(fallbackSecret)));
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Register ADO.NET repository
builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();

var app = builder.Build();

// Run DbUp database migrations if database connection string is present
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    try
    {
        var upgrader = DeployChanges.To
            .MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(System.Reflection.Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            app.Logger.LogWarning("Database migration failed or database unreachable: {Error}", result.Error);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Could not run migrations on startup: {Message}", ex.Message);
    }
}

// Exception Handling middleware
app.UseExceptionHandler();

// Security Headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("ArenaClientCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
