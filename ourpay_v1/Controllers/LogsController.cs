using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentApi.Services;
using PaymentApi.Models;

namespace PaymentApi.Controllers;

[ApiController]
[Route("api/v1/logs")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class LogsController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<LogsController> _logger;

    public LogsController(IAuditService auditService, ILogger<LogsController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit logs for a specific payment (Admin only)
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <returns>List of audit logs for the payment</returns>
    [HttpGet("{paymentId:guid}")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetPaymentLogs(Guid paymentId)
    {
        try
        {
            var logs = await _auditService.GetPaymentLogsAsync(paymentId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving logs for payment {PaymentId}", paymentId);
            return StatusCode(500, "An error occurred while retrieving the logs");
        }
    }
}
