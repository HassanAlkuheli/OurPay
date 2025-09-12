using System.ComponentModel.DataAnnotations;

namespace PaymentApi.DTOs;

public class WebhookDto
{
    public Guid WebhookId { get; set; }
    public Guid MerchantId { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string EventTypes { get; set; } = string.Empty; // JSON array of event types
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateWebhookRequest
{
    [Required]
    [Url]
    public string WebhookUrl { get; set; } = string.Empty;
    
    [Required]
    public string[] EventTypes { get; set; } = Array.Empty<string>();
    
    public bool IsActive { get; set; } = true;
}

public class UpdateWebhookRequest
{
    [Url]
    public string? WebhookUrl { get; set; }
    
    public string[]? EventTypes { get; set; }
    
    public bool? IsActive { get; set; }
}

public class WebhookEventDto
{
    public string EventType { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
    public Guid MerchantId { get; set; }
    public object Data { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string EventId { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
}

public class WebhookDeliveryAttempt
{
    public Guid AttemptId { get; set; }
    public Guid WebhookEventId { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public DateTime AttemptedAt { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
}

// Webhook Event Types
public static class WebhookEventTypes
{
    public const string PaymentCreated = "payment.created";
    public const string PaymentConfirmed = "payment.confirmed";
    public const string PaymentCancelled = "payment.cancelled";
    public const string PaymentExpired = "payment.expired";
    public const string PaymentFailed = "payment.failed";
    
    public static readonly string[] AllEventTypes = 
    {
        PaymentCreated,
        PaymentConfirmed,
        PaymentCancelled,
        PaymentExpired,
        PaymentFailed
    };
}
