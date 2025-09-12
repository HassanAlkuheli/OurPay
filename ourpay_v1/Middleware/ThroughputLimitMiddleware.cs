using PaymentApi.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;

namespace PaymentApi.Middleware
{

public class ThroughputLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ThroughputLimitSettings _settings;
    private readonly ILogger<ThroughputLimitMiddleware> _logger;
    private readonly SemaphoreSlim _semaphore;
    
    // Track requests per second
    private static readonly ConcurrentDictionary<string, int> _requestCounts = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lastReset = new();
    
    // Track connections per IP
    private static readonly ConcurrentDictionary<string, int> _connectionsPerIP = new();
    
    public ThroughputLimitMiddleware(
        RequestDelegate next,
        ThroughputLimitSettings settings,
        ILogger<ThroughputLimitMiddleware> logger)
    {
        _next = next;
        _settings = settings;
        _logger = logger;
        _semaphore = new SemaphoreSlim(_settings.MaxConcurrentRequests, _settings.MaxConcurrentRequests);
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_settings.EnableThroughputLimiting)
        {
            await _next(context);
            return;
        }
        
        var clientIP = GetClientIP(context);
        
        // Check connection limit per IP
        if (!await CheckConnectionLimitPerIP(context, clientIP))
        {
            return;
        }
        
        // Check global concurrent request limit
        if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            await SendResponse(context, 503, "CONCURRENT_LIMIT_EXCEEDED", 
                "Server is at maximum capacity. Please try again later.", 5);
            return;
        }
        
        try
        {
            // Check requests per second limit
            if (!await CheckRpsLimit(context, clientIP))
            {
                return;
            }
            
            // Set request timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds));
            var originalToken = context.RequestAborted;
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(originalToken, cts.Token).Token;
            
            context.RequestAborted = combinedToken;
            
            // Track connection
            _connectionsPerIP.AddOrUpdate(clientIP, 1, (key, value) => value + 1);
            
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                await SendResponse(context, 408, "REQUEST_TIMEOUT", 
                    $"Request timeout exceeded ({_settings.RequestTimeoutSeconds}s).", null);
            }
            finally
            {
                // Release connection tracking
                _connectionsPerIP.AddOrUpdate(clientIP, 0, (key, value) => Math.Max(0, value - 1));
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private string GetClientIP(HttpContext context)
    {
        // Try to get real IP from headers (load balancer, proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        
        var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIP))
        {
            return realIP;
        }
        
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
    
    private async Task<bool> CheckConnectionLimitPerIP(HttpContext context, string clientIP)
    {
        var currentConnections = _connectionsPerIP.GetValueOrDefault(clientIP, 0);
        
        if (currentConnections >= _settings.MaxConnectionsPerIP)
        {
            _logger.LogWarning("Connection limit exceeded for IP: {IP}, current: {Current}, limit: {Limit}", 
                clientIP, currentConnections, _settings.MaxConnectionsPerIP);
                
            await SendResponse(context, 429, "IP_CONNECTION_LIMIT_EXCEEDED", 
                $"Too many concurrent connections from your IP ({currentConnections}/{_settings.MaxConnectionsPerIP}).", 30);
            return false;
        }
        
        return true;
    }
    
    private async Task<bool> CheckRpsLimit(HttpContext context, string clientIP)
    {
        var currentSecond = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var key = $"global_rps_{currentSecond}";
        
        var currentCount = _requestCounts.AddOrUpdate(key, 1, (k, v) => v + 1);
        
        // Clean up old entries
        var cutoff = DateTime.UtcNow.AddSeconds(-10);
        var keysToRemove = _requestCounts.Keys
            .Where(k => k.StartsWith("global_rps_") && DateTime.TryParse(k.Replace("global_rps_", ""), out var dt) && dt < cutoff)
            .ToList();
            
        foreach (var oldKey in keysToRemove)
        {
            _requestCounts.TryRemove(oldKey, out _);
        }
        
        if (currentCount > _settings.MaxRequestsPerSecond)
        {
            _logger.LogWarning("Global RPS limit exceeded: {Current}/{Limit} for second {Second}", 
                currentCount, _settings.MaxRequestsPerSecond, currentSecond);
                
            await SendResponse(context, 429, "GLOBAL_RPS_LIMIT_EXCEEDED", 
                $"Global request rate limit exceeded ({currentCount}/{_settings.MaxRequestsPerSecond} req/sec).", 1);
            return false;
        }
        
        return true;
    }
    
    private async Task SendResponse(HttpContext context, int statusCode, string code, string message, int? retryAfter)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        
        if (retryAfter.HasValue)
        {
            context.Response.Headers.Add("Retry-After", retryAfter.Value.ToString());
        }
        
        var response = new
        {
            success = false,
            code = code,
            message = message,
            timestamp = DateTime.UtcNow,
            retryAfter = retryAfter
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
}
