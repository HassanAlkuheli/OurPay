using PaymentApi.Models;
using PaymentApi.Repositories;

namespace PaymentApi.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogActionAsync(Guid userId, string action, object? details = null, Guid? paymentId = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                PaymentId = paymentId,
                Action = action,
                Timestamp = DateTime.UtcNow
            };

            if (details != null)
            {
                auditLog.SetDetails(details);
            }

            await _unitOfWork.AuditLogs.CreateAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Audit log created: User {UserId}, Action {Action}, Payment {PaymentId}", 
                userId, action, paymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log for user {UserId}, action {Action}", userId, action);
            // Don't throw - audit logging should not break the main operation
        }
    }

    public async Task<IEnumerable<AuditLog>> GetPaymentLogsAsync(Guid paymentId)
    {
        try
        {
            return await _unitOfWork.AuditLogs.GetByPaymentIdAsync(paymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for payment {PaymentId}", paymentId);
            return new List<AuditLog>();
        }
    }
}
