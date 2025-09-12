using PaymentApi.Models;

namespace PaymentApi.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid userId);
    Task<bool> ExistsAsync(Guid userId);
    Task UpdateBalanceAsync(Guid userId, decimal newBalance);
}

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid paymentId);
    Task<Payment?> GetByIdWithDetailsAsync(Guid paymentId);
    Task<IEnumerable<Payment>> GetByMerchantIdAsync(Guid merchantId, int page = 1, int pageSize = 10);
    Task<IEnumerable<Payment>> GetAllAsync(int page = 1, int pageSize = 10);
    Task<int> GetTotalCountAsync(Guid? merchantId = null);
    Task<Payment> CreateAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task<IEnumerable<Payment>> GetExpiredPaymentsAsync();
    Task<bool> ExistsAsync(Guid paymentId);
}

public interface IAuditLogRepository
{
    Task<AuditLog> CreateAsync(AuditLog auditLog);
    Task<IEnumerable<AuditLog>> GetByPaymentIdAsync(Guid paymentId);
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 10);
    Task<IEnumerable<AuditLog>> GetAllAsync(int page = 1, int pageSize = 10);
}

public interface IWebhookRepository
{
    Task<Webhook?> GetByIdAsync(Guid webhookId);
    Task<Webhook?> GetWebhookByIdAndMerchantAsync(Guid webhookId, Guid merchantId);
    Task<IEnumerable<Webhook>> GetWebhooksByMerchantAsync(Guid merchantId);
    Task<IEnumerable<Webhook>> GetActiveWebhooksForEventAsync(Guid merchantId, string eventType);
    Task<Webhook> CreateAsync(Webhook webhook);
    Task UpdateAsync(Webhook webhook);
    Task DeleteAsync(Webhook webhook);
}

public interface IWebhookEventRepository
{
    Task<WebhookEvent?> GetByIdAsync(Guid webhookEventId);
    Task<WebhookEvent?> GetByEventIdAsync(string eventId);
    Task<IEnumerable<WebhookEvent>> GetUnprocessedEventsAsync();
    Task<WebhookEvent> CreateAsync(WebhookEvent webhookEvent);
    Task UpdateAsync(WebhookEvent webhookEvent);
}

public interface IWebhookDeliveryAttemptRepository
{
    Task<WebhookDeliveryAttempt> CreateAsync(WebhookDeliveryAttempt attempt);
    Task<IEnumerable<WebhookDeliveryAttempt>> GetByWebhookEventIdAsync(Guid webhookEventId);
}

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IPaymentRepository Payments { get; }
    IAuditLogRepository AuditLogs { get; }
    IWebhookRepository Webhooks { get; }
    IWebhookEventRepository WebhookEvents { get; }
    IWebhookDeliveryAttemptRepository WebhookDeliveryAttempts { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
