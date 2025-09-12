using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PaymentApi.Models;

[Table("Webhooks")]
public class Webhook
{
    [Key]
    public Guid WebhookId { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid MerchantId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string WebhookUrl { get; set; } = string.Empty;
    
    [Required]
    public string EventTypes { get; set; } = string.Empty; // JSON array
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    [ForeignKey("MerchantId")]
    public User Merchant { get; set; } = null!;
    
    // Helper methods
    public string[] GetEventTypes()
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(EventTypes) ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
    
    public void SetEventTypes(string[] eventTypes)
    {
        EventTypes = JsonSerializer.Serialize(eventTypes);
    }
}

[Table("WebhookEvents")]
public class WebhookEvent
{
    [Key]
    public Guid WebhookEventId { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid PaymentId { get; set; }
    
    [Required]
    public Guid MerchantId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    public string EventData { get; set; } = string.Empty; // JSON
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string EventId { get; set; } = string.Empty;
    
    public int Version { get; set; } = 1;
    
    public bool IsProcessed { get; set; } = false;
    public DateTime? ProcessedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("PaymentId")]
    public Payment Payment { get; set; } = null!;
    
    [ForeignKey("MerchantId")]
    public User Merchant { get; set; } = null!;
    
    public ICollection<WebhookDeliveryAttempt> DeliveryAttempts { get; set; } = new List<WebhookDeliveryAttempt>();
}

[Table("WebhookDeliveryAttempts")]
public class WebhookDeliveryAttempt
{
    [Key]
    public Guid AttemptId { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid WebhookEventId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string WebhookUrl { get; set; } = string.Empty;
    
    public int AttemptNumber { get; set; }
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Navigation property
    [ForeignKey("WebhookEventId")]
    public WebhookEvent WebhookEvent { get; set; } = null!;
}
