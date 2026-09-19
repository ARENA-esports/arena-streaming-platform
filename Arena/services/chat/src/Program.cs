using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ChatService.Repositories;
using ChatService.WebSockets;
using DbUp;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Problem Details for RFC 7807 standardized error responses
builder.Services.AddProblemDetails();

// Configure CORS policy for client access (same pattern as UserService)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:5173", "http://127.0.0.1:5173" };

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

// Dapper configuration
DefaultTypeMap.MatchNamesWithUnderscores = true;

// ── DI Registrations ──
builder.Services.AddSingleton<FactionChannelManager>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();

// ── JWT Authentication ──
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
            OnMessageReceived = context =>
            {
                // 1. Try cookie first (browser WebSocket handshakes send cookies automatically)
                if (context.Request.Cookies.TryGetValue("arena_access_token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                // 2. Fallback: query parameter for non-browser clients (e.g. wscat, mobile apps)
                else if (context.Request.Query.TryGetValue("token", out var queryToken)
                         && !string.IsNullOrWhiteSpace(queryToken))
                {
                    context.Token = queryToken!;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ── Run DbUp migrations against the Chat database before accepting requests ──
var connectionString = builder.Configuration.GetConnectionString("ChatDb")
    ?? throw new InvalidOperationException("ChatDb connection string is not configured.");

var upgrader = DeployChanges.To
    .MySqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(System.Reflection.Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();
if (!result.Successful)
{
    throw new Exception("Database migration failed: " + result.Error);
}

// ── HTTP Pipeline ──

// Exception Handling at the very top of the HTTP pipeline
app.UseExceptionHandler();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// CORS must run before Authentication and Authorization
app.UseCors("ArenaClientCors");

// WebSocket support — must be registered before auth so the pipeline can upgrade
app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();

// Map the WebSocket endpoint for faction chat
app.MapChatWebSocket();

app.MapControllers();

app.Run();
