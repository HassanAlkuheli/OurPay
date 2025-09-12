namespace PaymentApi.Configuration;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public class RedisSettings
{
    public const string SectionName = "RedisSettings";
    
    public string ConnectionString { get; set; } = string.Empty;
    public int DefaultDatabase { get; set; } = 0;
}

public class RateLimitSettings
{
    public const string SectionName = "RateLimitSettings";
    
    public int PaymentCreationPerMinute { get; set; } = 10;
    public int PaymentConfirmationPerMinute { get; set; } = 5;
    public int LoginAttemptsPerMinute { get; set; } = 5;
}

public class PaymentSettings
{
    public const string SectionName = "PaymentSettings";
    
    public string BaseUrl { get; set; } = string.Empty;
    public int DefaultExpirationMinutes { get; set; } = 1440; // 24 hours
    public int MaxExpirationMinutes { get; set; } = 10080; // 7 days
    public decimal MinAmount { get; set; } = 0.01m;
    public decimal MaxAmount { get; set; } = 10000.00m;
    public string[] SupportedCurrencies { get; set; } = { "USD", "EUR", "GBP" };
}

public class RabbitMQSettings
{
    public const string SectionName = "RabbitMQSettings";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public bool UseSSL { get; set; } = false;
    
    // Queue and Exchange Names
    public string PaymentWebhookExchange { get; set; } = "payment.webhooks";
    public string PaymentWebhookQueue { get; set; } = "payment.webhook.queue";
    public string PaymentWebhookRoutingKey { get; set; } = "payment.webhook";
}

public class WebhookSettings
{
    public const string SectionName = "WebhookSettings";
    
    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialRetryDelaySeconds { get; set; } = 5;
    public int MaxRetryDelaySeconds { get; set; } = 300;
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableWebhooks { get; set; } = true;
}

public class ThroughputLimitSettings
{
    public const string SectionName = "ThroughputLimitSettings";
    
    public bool EnableThroughputLimiting { get; set; } = true;
    public int MaxConcurrentRequests { get; set; } = 500;
    public int MaxRequestsPerSecond { get; set; } = 1000;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxRequestBodySizeMB { get; set; } = 30;
    public int MaxConnectionsPerIP { get; set; } = 50;
}

public class KestrelLimitSettings
{
    public const string SectionName = "KestrelLimitSettings";
    
    public int MaxConcurrentConnections { get; set; } = 1000;
    public int MaxConcurrentUpgradedConnections { get; set; } = 100;
    public long MaxRequestBodySize { get; set; } = 31_457_280; // 30MB
    public int MaxRequestBufferSize { get; set; } = 1_048_576; // 1MB
    public int MaxRequestHeaderCount { get; set; } = 100;
    public int MaxRequestHeadersTotalSize { get; set; } = 32_768; // 32KB
    public int KeepAliveTimeoutMinutes { get; set; } = 2;
    public int RequestHeadersTimeoutSeconds { get; set; } = 30;
}
