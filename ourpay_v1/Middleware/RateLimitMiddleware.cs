using PaymentApi.Configuration;
using PaymentApi.Services;

namespace PaymentApi.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitSettings _rateLimitSettings;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(
        RequestDelegate next,
        RateLimitSettings rateLimitSettings,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _rateLimitSettings = rateLimitSettings;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var cacheService = context.RequestServices.GetRequiredService<ICacheService>();
        
        if (await IsRateLimited(context, cacheService))
        {
            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                success = false,
                message = "Rate limit exceeded. Please try again later.",
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            return;
        }

        await _next(context);
    }

    private async Task<bool> IsRateLimited(HttpContext context, ICacheService cacheService)
    {
        try
        {
            var endpoint = context.Request.Path.Value?.ToLower();
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            string? rateLimitKey = null;
            int limit = 0;

            // Determine rate limit based on endpoint
            if (endpoint?.Contains("/api/v1/payments") == true && context.Request.Method == "POST")
            {
                // Payment creation limit
                rateLimitKey = $"rate_limit:payment_create:{userId ?? clientIp}";
                limit = _rateLimitSettings.PaymentCreationPerMinute;
            }
            else if (endpoint?.Contains("/confirm") == true && context.Request.Method == "POST")
            {
                // Payment confirmation limit
                rateLimitKey = $"rate_limit:payment_confirm:{userId ?? clientIp}";
                limit = _rateLimitSettings.PaymentConfirmationPerMinute;
            }
            else if (endpoint?.Contains("/api/v1/auth/login") == true)
            {
                // Login attempts limit
                rateLimitKey = $"rate_limit:login:{clientIp}";
                limit = _rateLimitSettings.LoginAttemptsPerMinute;
            }

            if (rateLimitKey != null && limit > 0)
            {
                var currentCount = await cacheService.GetCounterAsync(rateLimitKey);
                
                if (currentCount >= limit)
                {
                    _logger.LogWarning("Rate limit exceeded for key: {RateLimitKey}, count: {Count}, limit: {Limit}", 
                        rateLimitKey, currentCount, limit);
                    return true;
                }

                // Increment counter
                await cacheService.IncrementAsync(rateLimitKey, TimeSpan.FromMinutes(1));
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit");
            return false; // Don't block on error
        }
    }
}
