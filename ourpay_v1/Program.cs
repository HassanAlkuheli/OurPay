using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Identity;
using PaymentApi.Configuration;
using PaymentApi.Data;
using PaymentApi.Middleware;
using PaymentApi.Models;
using PaymentApi.Repositories;
using PaymentApi.Services;
using Serilog;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel limits first
var kestrelLimits = builder.Configuration.GetSection(KestrelLimitSettings.SectionName).Get<KestrelLimitSettings>() ?? new KestrelLimitSettings();
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxConcurrentConnections = kestrelLimits.MaxConcurrentConnections;
    options.Limits.MaxConcurrentUpgradedConnections = kestrelLimits.MaxConcurrentUpgradedConnections;
    options.Limits.MaxRequestBodySize = kestrelLimits.MaxRequestBodySize;
    options.Limits.MaxRequestBufferSize = kestrelLimits.MaxRequestBufferSize;
    options.Limits.MaxRequestHeaderCount = kestrelLimits.MaxRequestHeaderCount;
    options.Limits.MaxRequestHeadersTotalSize = kestrelLimits.MaxRequestHeadersTotalSize;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(kestrelLimits.KeepAliveTimeoutMinutes);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(kestrelLimits.RequestHeadersTimeoutSeconds);
});

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/payment-api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "paymentapi", serviceVersion: "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["service.instance.id"] = Environment.MachineName
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, httpRequest) =>
            {
                activity.SetTag("http.request.body.size", httpRequest.ContentLength);
            };
            options.EnrichWithHttpResponse = (activity, httpResponse) =>
            {
                activity.SetTag("http.response.body.size", httpResponse.ContentLength);
            };
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://tempo:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

// Add services to the container
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Payment API", 
        Version = "v1",
        Description = "A comprehensive Payment by Link API"
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Configuration sections
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
var redisSettings = builder.Configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>() ?? new RedisSettings();
var rateLimitSettings = builder.Configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();
var paymentSettings = builder.Configuration.GetSection(PaymentSettings.SectionName).Get<PaymentSettings>() ?? new PaymentSettings();
var rabbitMQSettings = builder.Configuration.GetSection(RabbitMQSettings.SectionName).Get<RabbitMQSettings>() ?? new RabbitMQSettings();
var webhookSettings = builder.Configuration.GetSection(WebhookSettings.SectionName).Get<WebhookSettings>() ?? new WebhookSettings();
var throughputLimitSettings = builder.Configuration.GetSection(ThroughputLimitSettings.SectionName).Get<ThroughputLimitSettings>() ?? new ThroughputLimitSettings();
var kestrelLimitSettings = builder.Configuration.GetSection(KestrelLimitSettings.SectionName).Get<KestrelLimitSettings>() ?? new KestrelLimitSettings();

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(redisSettings);
builder.Services.AddSingleton(rateLimitSettings);
builder.Services.AddSingleton(paymentSettings);
builder.Services.AddSingleton(rabbitMQSettings);
builder.Services.AddSingleton(webhookSettings);
builder.Services.AddSingleton(throughputLimitSettings);
builder.Services.AddSingleton(kestrelLimitSettings);
builder.Services.AddSingleton(webhookSettings);

// Database - Use SQLite for development, PostgreSQL for production
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    if (connectionString.Contains("Data Source"))
    {
        // SQLite for local development
        options.UseSqlite(connectionString);
    }
    else
    {
        // PostgreSQL for production
        options.UseNpgsql(connectionString);
    }
});

// Identity configuration
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    
    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<PaymentDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier
    };
});

builder.Services.AddAuthorization();

// Redis - Enable for Docker environment
if (builder.Environment.EnvironmentName == "Docker" || 
    builder.Configuration.GetValue<string>("RedisSettings:ConnectionString")?.Contains("redis") == true)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        ConnectionMultiplexer.Connect(redisSettings.ConnectionString));
}

// RabbitMQ - Enable for Docker environment
if (builder.Environment.EnvironmentName == "Docker" || 
    builder.Configuration.GetValue<string>("RabbitMQSettings:HostName")?.Contains("rabbitmq") == true)
{
    builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
}

// HTTP Client for webhooks
builder.Services.AddHttpClient();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Repository pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IWebhookRepository, WebhookRepository>();
builder.Services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
builder.Services.AddScoped<IWebhookDeliveryAttemptRepository, WebhookDeliveryAttemptRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Webhook and Cache services - Use real services in Docker, mocks otherwise
if (builder.Environment.EnvironmentName == "Docker" || 
    builder.Configuration.GetValue<string>("RabbitMQSettings:HostName")?.Contains("rabbitmq") == true)
{
    builder.Services.AddScoped<IWebhookService, WebhookService>();
    builder.Services.AddScoped<ICacheService, CacheService>();
}
else
{
    builder.Services.AddScoped<IWebhookService, MockWebhookService>();
    builder.Services.AddScoped<ICacheService, MockCacheService>();
}

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Background services - Enable for Docker environment
if (builder.Environment.EnvironmentName == "Docker" || 
    builder.Configuration.GetValue<string>("RabbitMQSettings:HostName")?.Contains("rabbitmq") == true)
{
    builder.Services.AddHostedService<WebhookBackgroundService>();
}

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment API V1");
    });
}
else
{
    // Enable Swagger in production for testing purposes
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment API V1");
        c.RoutePrefix = ""; // Make Swagger available at root
    });
}

// Only use HTTPS redirection in production with proper SSL setup
// Disabled for development and containerized environments
// if (!app.Environment.IsDevelopment())
// {
//     app.UseHttpsRedirection();
// }

app.UseCors("AllowAll");

// Custom middleware - temporarily disabled for debugging
// app.UseMiddleware<ErrorHandlingMiddleware>();
// app.UseMiddleware<ThroughputLimitMiddleware>();
// app.UseMiddleware<RateLimitMiddleware>(); // Disabled - uses Redis

app.UseAuthentication();
app.UseAuthorization();

// Add Prometheus metrics endpoint
app.UseHttpMetrics();
app.MapMetrics();

app.MapControllers();

// Initialize database with proper error handling
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    
    try
    {
        context.Database.EnsureCreated();
        Log.Information("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while creating the database");
        // Don't crash the app if database fails in container
    }
}

try
{
    Log.Information("Starting API server on http://localhost:5262");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
    throw;
}

// Make the Program class accessible for testing
public partial class Program { }
