using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentApi.Models;

public class Payment
{
    [Key]
    public Guid PaymentId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid MerchantId { get; set; }

    public Guid? CustomerId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(MerchantId))]
    public virtual User Merchant { get; set; } = null!;

    [ForeignKey(nameof(CustomerId))]
    public virtual User? Customer { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public enum PaymentStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Cancelled = 3,
    Expired = 4
}
