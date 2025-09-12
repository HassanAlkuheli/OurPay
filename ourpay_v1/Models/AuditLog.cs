using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PaymentApi.Models;

public class AuditLog
{
    [Key]
    public Guid LogId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    public Guid? PaymentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string Details { get; set; } = "{}";

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(PaymentId))]
    public virtual Payment? Payment { get; set; }

    // Helper methods for JSON serialization
    public void SetDetails<T>(T details)
    {
        Details = JsonSerializer.Serialize(details);
    }

    public T? GetDetails<T>()
    {
        if (string.IsNullOrEmpty(Details) || Details == "{}")
            return default;
        
        try
        {
            return JsonSerializer.Deserialize<T>(Details);
        }
        catch
        {
            return default;
        }
    }
}
