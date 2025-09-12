using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;
using PaymentApi.Models;
using System.Text.Json;

namespace PaymentApi.Repositories;

public class WebhookRepository : IWebhookRepository
{
    private readonly PaymentDbContext _context;

    public WebhookRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Webhook?> GetByIdAsync(Guid webhookId)
    {
        return await _context.Webhooks
            .Include(w => w.Merchant)
            .FirstOrDefaultAsync(w => w.WebhookId == webhookId);
    }

    public async Task<Webhook?> GetWebhookByIdAndMerchantAsync(Guid webhookId, Guid merchantId)
    {
        return await _context.Webhooks
            .Include(w => w.Merchant)
            .FirstOrDefaultAsync(w => w.WebhookId == webhookId && w.MerchantId == merchantId);
    }

    public async Task<IEnumerable<Webhook>> GetWebhooksByMerchantAsync(Guid merchantId)
    {
        return await _context.Webhooks
            .Where(w => w.MerchantId == merchantId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Webhook>> GetActiveWebhooksForEventAsync(Guid merchantId, string eventType)
    {
        var webhooks = await _context.Webhooks
            .Where(w => w.MerchantId == merchantId && w.IsActive)
            .ToListAsync();

        // Filter by event type using the JSON column
        return webhooks.Where(w => w.GetEventTypes().Contains(eventType));
    }

    public async Task<Webhook> CreateAsync(Webhook webhook)
    {
        _context.Webhooks.Add(webhook);
        return webhook;
    }

    public async Task UpdateAsync(Webhook webhook)
    {
        _context.Webhooks.Update(webhook);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Webhook webhook)
    {
        _context.Webhooks.Remove(webhook);
        await Task.CompletedTask;
    }
}

public class WebhookEventRepository : IWebhookEventRepository
{
    private readonly PaymentDbContext _context;

    public WebhookEventRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<WebhookEvent?> GetByIdAsync(Guid webhookEventId)
    {
        return await _context.WebhookEvents
            .Include(we => we.Payment)
            .Include(we => we.Merchant)
            .Include(we => we.DeliveryAttempts)
            .FirstOrDefaultAsync(we => we.WebhookEventId == webhookEventId);
    }

    public async Task<WebhookEvent?> GetByEventIdAsync(string eventId)
    {
        return await _context.WebhookEvents
            .Include(we => we.Payment)
            .Include(we => we.Merchant)
            .Include(we => we.DeliveryAttempts)
            .FirstOrDefaultAsync(we => we.EventId == eventId);
    }

    public async Task<IEnumerable<WebhookEvent>> GetUnprocessedEventsAsync()
    {
        return await _context.WebhookEvents
            .Where(we => !we.IsProcessed)
            .OrderBy(we => we.Timestamp)
            .Take(100) // Limit to prevent memory issues
            .ToListAsync();
    }

    public async Task<WebhookEvent> CreateAsync(WebhookEvent webhookEvent)
    {
        _context.WebhookEvents.Add(webhookEvent);
        return webhookEvent;
    }

    public async Task UpdateAsync(WebhookEvent webhookEvent)
    {
        _context.WebhookEvents.Update(webhookEvent);
        await Task.CompletedTask;
    }
}

public class WebhookDeliveryAttemptRepository : IWebhookDeliveryAttemptRepository
{
    private readonly PaymentDbContext _context;

    public WebhookDeliveryAttemptRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<WebhookDeliveryAttempt> CreateAsync(WebhookDeliveryAttempt attempt)
    {
        _context.WebhookDeliveryAttempts.Add(attempt);
        return attempt;
    }

    public async Task<IEnumerable<WebhookDeliveryAttempt>> GetByWebhookEventIdAsync(Guid webhookEventId)
    {
        return await _context.WebhookDeliveryAttempts
            .Where(wda => wda.WebhookEventId == webhookEventId)
            .OrderByDescending(wda => wda.AttemptedAt)
            .ToListAsync();
    }
}
