using Microsoft.EntityFrameworkCore.Storage;
using PaymentApi.Data;

namespace PaymentApi.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly PaymentDbContext _context;
    private IDbContextTransaction? _transaction;
    
    private IUserRepository? _users;
    private IPaymentRepository? _payments;
    private IAuditLogRepository? _auditLogs;
    private IWebhookRepository? _webhooks;
    private IWebhookEventRepository? _webhookEvents;
    private IWebhookDeliveryAttemptRepository? _webhookDeliveryAttempts;

    public UnitOfWork(PaymentDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);
    public IWebhookRepository Webhooks => _webhooks ??= new WebhookRepository(_context);
    public IWebhookEventRepository WebhookEvents => _webhookEvents ??= new WebhookEventRepository(_context);
    public IWebhookDeliveryAttemptRepository WebhookDeliveryAttempts => _webhookDeliveryAttempts ??= new WebhookDeliveryAttemptRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
