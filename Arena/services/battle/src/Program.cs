using System.Security.Claims;
using System.Text;
using Dapper;
using DbUp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Polly;
using BattleEconomyService.Configuration;
using BattleEconomyService.Repositories;
using BattleEconomyService.Services;

var builder = WebApplication.CreateBuilder(args);

// Dapper configuration for underscore column-to-property mapping
DefaultTypeMap.MatchNamesWithUnderscores = true;

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

// Configuration Options
builder.Services.Configure<EconomyOptions>(
    builder.Configuration.GetSection(EconomyOptions.SectionName));
builder.Services.Configure<StreamServiceOptions>(
    builder.Configuration.GetSection(StreamServiceOptions.SectionName));

// Singleton TimeProvider for deterministic time operations
builder.Services.AddSingleton(TimeProvider.System);

// Register Repositories
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ICoinTransactionRepository, CoinTransactionRepository>();

// Register StreamService HTTP Client with Polly resilience (SCRUM-118)
builder.Services.AddHttpClient<IStreamServiceClient, StreamServiceClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<StreamServiceOptions>>().Value;
    var baseUrl = options.BaseUrl.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
})
.AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(
    retryCount: 1,
    sleepDurationProvider: _ => TimeSpan.FromMilliseconds(100)
));

// Register Services and Extension Points
builder.Services.AddScoped<IWatchTickService, WatchTickService>();
builder.Services.AddScoped<IStreamLivenessValidator, HttpStreamLivenessValidator>();
builder.Services.AddScoped<ICoinCapPolicy, SlidingWindowCoinCapPolicy>();
builder.Services.AddScoped<ICoinEarnedEventPublisher, NoOpCoinEarnedEventPublisher>();

// Restrictive CORS policy for Arena web clients
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

// Swagger / OpenAPI documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Arena Battle & Economy Service API",
        Version = "v1",
        Description = "Microservice managing viewer coin economy, watch-ticks, and battle mechanics."
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

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// JWT Authentication per jwt-spec.md
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? "Arena_Secret_Key_For_Jwt_Token_Signing_2026_SE3022_Production_Grade!";
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
                if (context.Request.Cookies.TryGetValue("arena_access_token", out var token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Run DbUp database migrations on startup if configured
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    try
    {
        EnsureDatabase.For.MySqlDatabase(connectionString);

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

// HTTP pipeline configuration
app.UseExceptionHandler();

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

// Needed for WebApplicationFactory integration test support if used
public partial class Program { }
