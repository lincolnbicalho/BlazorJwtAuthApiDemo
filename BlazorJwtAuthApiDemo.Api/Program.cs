using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using BlazorJwtAuthApiDemo.Api.Services;
using BlazorJwtAuthApiDemo.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// ===== Add services to the container =====

// Add controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // WHY: Serialize enums as strings for better readability
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Add Swagger/OpenAPI with JWT configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Blazor JWT Auth Demo API",
        Version = "v1",
        Description = "Demo API for Blazor Server + API JWT Authentication"
    });

    // WHY: Configure Swagger to accept JWT tokens for testing
    // HOW: Adds "Authorize" button in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ===== Configure JWT Authentication =====

var jwtSection = builder.Configuration.GetSection("JWT");
var secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT:SecretKey is required in appsettings.json");
var issuer = jwtSection["Issuer"] ?? "BlazorJwtAuthDemo";
var audience = jwtSection["Audience"] ?? "BlazorJwtAuthDemo";

builder.Services.AddAuthentication(options =>
{
    // WHY: Set JWT Bearer as the default authentication scheme
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // WHY: Configure token validation parameters
    // HOW: These must match the parameters used when generating tokens
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // WHY: No grace period for token expiration
    };
});

// ===== Configure CORS =====

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorApp", policy =>
    {
        // WHY: Allow any origin to call this API (development mode)
        // HOW: SetIsOriginAllowed allows all origins while supporting credentials
        // NOTE: In production, replace with specific origins for security
        policy.SetIsOriginAllowed(_ => true)  // Allow any origin
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();  // Required for JWT authentication
    });
});

// ===== Register application services =====

// WHY: Register as Singleton because it's thread-safe and doesn't need per-request instances
builder.Services.AddSingleton<MockUserRepository>();

// WHY: Register as Scoped to match the HTTP request lifetime
builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

// ===== Configure the HTTP request pipeline =====

// WHY: Enable Swagger only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Blazor JWT Auth Demo API v1");
        options.RoutePrefix = "swagger"; // Access at /swagger
    });
}

app.UseHttpsRedirection();

// WHY: CORS must come before authentication
app.UseCors("BlazorApp");

// WHY: Authentication must come before authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// WHY: Add a simple root endpoint for testing
app.MapGet("/", () => new
{
    message = "Blazor JWT Auth Demo API",
    version = "1.0.0",
    timestamp = DateTime.UtcNow,
    endpoints = new
    {
        swagger = "/swagger",
        auth = "/api/auth",
        test = "/api/auth/test"
    }
}).WithName("Root");

app.Run();

// WHY: Make Program class public for testing
public partial class Program { }
