using System.Security.Claims;   // provide standard claim types as ClaimTypes.Role
using System.text;      // provides character encoding tool to convert strings into byte rates
using Microsoft.AspNetCore.Authentication.JwtBearer;    // provide authentication scheme constants, JWT options
using Microsoft.IdentityModel.Tokens;           // contain cryptographic keys, validation parameter
using Microsoft.OpenAPI.Models;                 // provide types to configure swagger ui dialog interactive Bearer token testing
using StreamService.Repositories;


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
    c.AddSecurityRequirement(new OpenApiSecurityRequirement   // apply security schemas across all API endpoints
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference            // link requirement directly to bearer security definition
                {
                    Type=ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()       // specified no OAuth2 scope limitation need for basic JWT authentication
        }
    });
});






// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
