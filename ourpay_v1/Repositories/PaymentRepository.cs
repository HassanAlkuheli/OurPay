using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;
using PaymentApi.Models;

namespace PaymentApi.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid paymentId)
    {
        return await _context.Payments.FindAsync(paymentId);
    }

    public async Task<Payment?> GetByIdWithDetailsAsync(Guid paymentId)
    {
        return await _context.Payments
            .Include(p => p.Merchant)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
    }

    public async Task<IEnumerable<Payment>> GetByMerchantIdAsync(Guid merchantId, int page = 1, int pageSize = 10)
    {
        return await _context.Payments
            .Include(p => p.Merchant)
            .Include(p => p.Customer)
            .Where(p => p.MerchantId == merchantId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Payments
            .Include(p => p.Merchant)
            .Include(p => p.Customer)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(Guid? merchantId = null)
    {
        var query = _context.Payments.AsQueryable();
        
        if (merchantId.HasValue)
        {
            query = query.Where(p => p.MerchantId == merchantId.Value);
        }
        
        return await query.CountAsync();
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        return payment;
    }

    public async Task UpdateAsync(Payment payment)
    {
        payment.UpdatedAt = DateTime.UtcNow;
        _context.Payments.Update(payment);
    }

    public async Task<IEnumerable<Payment>> GetExpiredPaymentsAsync()
    {
        return await _context.Payments
            .Where(p => p.Status == PaymentStatus.Pending && p.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid paymentId)
    {
        return await _context.Payments.AnyAsync(p => p.PaymentId == paymentId);
    }
}
