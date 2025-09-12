using System.ComponentModel.DataAnnotations;

namespace PaymentApi.DTOs;

public class CreatePaymentRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Expiration time must be at least 1 minute")]
    public int ExpiresInMinutes { get; set; } = 1440; // Default 24 hours
}

public class CreatePaymentResponse
{
    public Guid PaymentId { get; set; }
    public string PaymentLink { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentDto
{
    public Guid PaymentId { get; set; }
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ConfirmPaymentRequest
{
    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class ConfirmPaymentResponse
{
    public Guid PaymentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

public class PaymentListResponse
{
    public IEnumerable<PaymentDto> Payments { get; set; } = new List<PaymentDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
