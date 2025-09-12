using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentApi.DTOs;
using PaymentApi.Services;
using System.Security.Claims;

namespace PaymentApi.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new payment link (Merchant only)
    /// </summary>
    /// <param name="request">Payment details</param>
    /// <returns>Payment creation response with payment link</returns>
    [HttpPost]
    [Authorize(Roles = "Merchant")]
    public async Task<ActionResult<ApiResponse<CreatePaymentResponse>>> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(ApiResponse<CreatePaymentResponse>.ErrorResponse(errors));
        }

        var merchantId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _paymentService.CreatePaymentAsync(merchantId, request);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get payment details (Public endpoint)
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <returns>Payment details</returns>
    [HttpGet("{paymentId:guid}")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> GetPayment(Guid paymentId)
    {
        var result = await _paymentService.GetPaymentAsync(paymentId);

        if (!result.Success && result.Message == "Payment not found")
        {
            return NotFound(result);
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Confirm payment (Customer only)
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="request">Confirmation details with idempotency key</param>
    /// <returns>Payment confirmation response</returns>
    [HttpPost("{paymentId:guid}/confirm")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<ConfirmPaymentResponse>>> ConfirmPayment(
        Guid paymentId, 
        [FromBody] ConfirmPaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(ApiResponse<ConfirmPaymentResponse>.ErrorResponse(errors));
        }

        var customerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _paymentService.ConfirmPaymentAsync(paymentId, customerId, request);

        if (!result.Success && result.Message == "Payment not found")
        {
            return NotFound(result);
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Cancel payment (Merchant/Admin only)
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <returns>Cancellation response</returns>
    [HttpPost("{paymentId:guid}/cancel")]
    [Authorize(Roles = "Merchant,Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelPayment(Guid paymentId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)!.Value;
        
        var result = await _paymentService.CancelPaymentAsync(paymentId, userId, userRole);

        if (!result.Success && result.Message == "Payment not found")
        {
            return NotFound(result);
        }

        if (!result.Success && result.Message.Contains("don't have permission"))
        {
            return Forbid();
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get payments list (Merchant gets own, Admin gets all)
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated payments list</returns>
    [HttpGet]
    [Authorize(Roles = "Merchant,Admin")]
    public async Task<ActionResult<ApiResponse<PaymentListResponse>>> GetPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)!.Value;
        
        var merchantId = userRole == "Merchant" ? userId : (Guid?)null;
        
        var result = await _paymentService.GetPaymentsAsync(merchantId, userRole, page, pageSize);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}
