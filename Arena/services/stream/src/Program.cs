using System.Security.Claims;   // provide standard claim types as ClaimTypes.Role
using System.Text;      // provides character encoding tool to convert strings into byte rates
using Microsoft.AspNetCore.Authentication.JwtBearer;    // provide authentication scheme constants, JWT options
using Microsoft.IdentityModel.Tokens;           // contain cryptographic keys, validation parameter
using Microsoft.OpenApi;                 // provide types to configure swagger ui dialog interactive Bearer token testing
using StreamService.Repositories;
using StreamService.Services;
using DbUp;

var builder = WebApplication.CreateBuilder(args);   // initialize configuration sources

// Add services to the container.

builder.Services.AddControllers();      // register controller discovery and model binder to dependency injection container
builder.Services.AddEndpointsApiExplorer();     // enable API metadata discovery to swagger/openAPI
builder.Services.AddSwaggerGen(c =>         // register swagger generation services
{
    c.SwaggerDoc("v1", new OpenApiInfo      // define version identifier and set human readable title appear top of swagger ui page
    {
        Title = "Arena Stream Service API",
        Version = "v1"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme     // register named security scheme configuration in OpenApi doc
    {
        Description = "Enter JWT Bearer token: Bearer {your_jwt_token}",
        Name = "Authorization",             // specify exact HTTP request header name to populate
        In = ParameterLocation.Header,      // direct swagger to pass token inside HTTP request headers
        Type = SecuritySchemeType.ApiKey,   // inform swagger ui to display text input modal when user click "authorize" button
        Scheme = "Bearer"                   // standard HTTP authorization scheme name
    });
    // apply security requirement globally in swagger
    // Swashbuckle 10+ lambda requirement linking schema to OpenApiDocument
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});
//    c.AddSecurityRequirement(new OpenApiSecurityRequirement   // apply security schemas across all API endpoints
//     {
//         {
//             [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>() // changed Array.Empty<string>() to new List<string>() to match Dictionary<OpenApiSecuritySchemeReference, List<string>>
//         }
//     });
// }); // updated to Swashbuckle 10+ constructor approach

/*
    Grab the JWT secret, issuer, and audience from configuration.
    If they are missing, use safe defaults.
*/
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");     // look up for secret value under JwtSettings in config sources
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]
    ?? "Arena.UserService";                                             // read issuer from configuration or if key is missing go with default
var jwtAudience = builder.Configuration["JwtSettings:Audience"]
    ?? "Arena.Platform";                                                // only accept tokens that meant for Arena.Platform


/* register authentication system with default JWT Bearer Scheme */

/*
    inject authentication services and configure bearer as default scheme.
    ASP.NET use to inspect incoming HTTP request
*/
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>        // register middleware to handle JWT tokens with lambda func
    {
        options.TokenValidationParameters = new TokenValidationParameters       // configure cryptographic token validation parameters
        {
            ValidateIssuerSigningKey = true,                                     // ensure incoming token signed with trusted secret and hasn't tampered
            /*
                convert string secret into byte array and wrap it with symmetric security key for HS256 verification
            */
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),  // convert jwt string into UTF8 byte array
            ValidateIssuer = true,                                              // check token's iss claim
            ValidIssuer = jwtIssuer,                                    // only accept token come from valid user
            ValidateAudience = true,        // check token's aud claim
            ValidAudience= jwtAudience,     // only accept tokens from valid audience ensuring incoming tokens are from arena
            ValidateLifetime = true,        // ensure current time of token between valid time. reject expired and not yet valid
            RoleClaimType = ClaimTypes.Role,// map standard role claim jwt payload directly from ASP.NET
            ClockSkew = TimeSpan.Zero       // disable 5 min clock drift to expire tokens in correct time
        };
    });

builder.Services.AddAuthorization();    // register authorization services
builder.Services.AddScoped<IMatchRepository, MatchRepository>();    // dependency injection
builder.Services.AddScoped<IStreamRepository, StreamRepository>();    // dependency injection
builder.Services.AddScoped<IWebhookLogRepository, WebhookLogRepository>();//di
builder.Services.AddScoped<ITwitchEventSubValidator, TwitchEventSubValidator>();//di


var app = builder.Build();      // compile service registrations and create runnable web application

// Run DbUp migrations against the Stream database before accepting requests
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection string is not configured.");

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();       // enable API documentation endpoints in dev mode
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();    // read jwt token from auth header, validate, assign authenticate identity ti http user
app.UseAuthorization();     // evaluate endpoint authorization rules

app.MapControllers();       // map incoming http request URLs directly to the route

app.Run();                  // start kestrel web server
