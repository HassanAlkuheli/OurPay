using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;
using PaymentApi.Models;

namespace PaymentApi.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly PaymentDbContext _context;

    public AuditLogRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog> CreateAsync(AuditLog auditLog)
    {
        _context.AuditLogs.Add(auditLog);
        return auditLog;
    }

    public async Task<IEnumerable<AuditLog>> GetByPaymentIdAsync(Guid paymentId)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.PaymentId == paymentId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Payment)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Payment)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
