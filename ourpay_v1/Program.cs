using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

// Simple API endpoints for testing
app.MapGet("/", () => new { 
    message = "🚀 OurPay API - Global Access Ready!", 
    version = "1.0.0",
    timestamp = DateTime.UtcNow,
    status = "online"
});

app.MapGet("/health", () => new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    server = "Oracle Cloud ARM64"
});

app.MapPost("/api/v1/auth/register", ([FromBody] RegisterRequest request) => {
    // Simulate registration
    if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
    {
        return Results.BadRequest(new { error = "Email and password required" });
    }
    
    return Results.Ok(new { 
        message = "Registration successful!",
        userId = Guid.NewGuid(),
        email = request.Email,
        role = request.Role ?? "customer",
        timestamp = DateTime.UtcNow
    });
});

app.MapPost("/api/v1/auth/login", ([FromBody] LoginRequest request) => {
    // Simulate login
    if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
    {
        return Results.BadRequest(new { error = "Email and password required" });
    }
    
    var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{request.Email}:{DateTime.UtcNow.Ticks}"));
    
    return Results.Ok(new { 
        message = "Login successful!",
        accessToken = token,
        refreshToken = Guid.NewGuid().ToString(),
        expiresIn = 900, // 15 minutes
        tokenType = "Bearer",
        timestamp = DateTime.UtcNow
    });
});

app.MapPost("/api/v1/payments", ([FromBody] PaymentRequest request) => {
    // Simulate payment creation
    var paymentId = Guid.NewGuid();
    
    return Results.Ok(new { 
        message = "Payment link created successfully!",
        paymentId = paymentId,
        amount = request.Amount,
        currency = request.Currency ?? "USD",
        description = request.Description ?? "Payment",
        paymentLink = $"https://newer-rotation-disappointed-musical.trycloudflare.com/pay/{paymentId}",
        status = "pending",
        expiresAt = DateTime.UtcNow.AddHours(24),
        timestamp = DateTime.UtcNow
    });
});

app.MapPost("/api/v1/payments/{id}/confirm", (string id, [FromBody] ConfirmPaymentRequest request) => {
    // Simulate payment confirmation
    return Results.Ok(new { 
        message = "Payment confirmed successfully!",
        paymentId = id,
        status = "completed",
        amount = request.Amount ?? 100.00m,
        transactionId = Guid.NewGuid(),
        timestamp = DateTime.UtcNow
    });
});

app.MapGet("/api/v1/payments", () => {
    // Return sample payments
    return Results.Ok(new { 
        payments = new[] {
            new { 
                id = Guid.NewGuid(),
                amount = 50.00m,
                currency = "USD",
                status = "completed",
                createdAt = DateTime.UtcNow.AddHours(-2)
            },
            new { 
                id = Guid.NewGuid(),
                amount = 75.50m,
                currency = "USD", 
                status = "pending",
                createdAt = DateTime.UtcNow.AddMinutes(-30)
            }
        },
        totalCount = 2
    });
});

Console.WriteLine("🚀 OurPay API starting...");
Console.WriteLine("🌍 Global access via Cloudflare Tunnel");
Console.WriteLine("📱 Mobile-friendly endpoints ready");

app.Run();

// DTOs
public record RegisterRequest(string Email, string Password, string? Role);
public record LoginRequest(string Email, string Password);
public record PaymentRequest(decimal Amount, string? Currency, string? Description);
public record ConfirmPaymentRequest(decimal? Amount);
