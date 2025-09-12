using PaymentApi.Services;
using PaymentApi.DTOs;

namespace PaymentApi.Services;

public class MockWebhookService : IWebhookService
{
    public Task<ApiResponse<WebhookDto>> CreateWebhookAsync(Guid merchantId, CreateWebhookRequest request)
    {
        return Task.FromResult(new ApiResponse<WebhookDto>
        {
            Success = true,
            Data = new WebhookDto
            {
                WebhookId = Guid.NewGuid(),
                MerchantId = merchantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        });
    }

    public Task<ApiResponse<IEnumerable<WebhookDto>>> GetMerchantWebhooksAsync(Guid merchantId)
    {
        return Task.FromResult(new ApiResponse<IEnumerable<WebhookDto>>
        {
            Success = true,
            Data = new List<WebhookDto>()
        });
    }

    public Task<ApiResponse<WebhookDto>> GetWebhookAsync(Guid webhookId, Guid merchantId)
    {
        return Task.FromResult(new ApiResponse<WebhookDto>
        {
            Success = false,
            Message = "Webhook not found"
        });
    }

    public Task<ApiResponse<WebhookDto>> UpdateWebhookAsync(Guid webhookId, Guid merchantId, UpdateWebhookRequest request)
    {
        return Task.FromResult(new ApiResponse<WebhookDto>
        {
            Success = true,
            Data = new WebhookDto
            {
                WebhookId = webhookId,
                MerchantId = merchantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            }
        });
    }

    public Task<ApiResponse<bool>> DeleteWebhookAsync(Guid webhookId, Guid merchantId)
    {
        return Task.FromResult(new ApiResponse<bool>
        {
            Success = true,
            Data = true
        });
    }

    public Task PublishWebhookEventAsync(string eventType, Guid paymentId, Guid merchantId, object eventData)
    {
        // Mock implementation - no actual webhook sending
        return Task.CompletedTask;
    }

    public Task ProcessWebhookEventAsync(WebhookEventDto webhookEvent)
    {
        // Mock implementation - no actual webhook processing
        return Task.CompletedTask;
    }
}
