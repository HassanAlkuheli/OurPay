using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentApi.DTOs;
using PaymentApi.Services;
using System.Security.Claims;

namespace PaymentApi.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
[Authorize(Roles = "Merchant")]
[Produces("application/json")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IWebhookService webhookService, ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new webhook for the merchant
    /// </summary>
    /// <param name="request">Webhook configuration</param>
    /// <returns>Created webhook details</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<WebhookDto>>> CreateWebhook([FromBody] CreateWebhookRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(ApiResponse<WebhookDto>.ErrorResponse(errors));
        }

        var merchantId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _webhookService.CreateWebhookAsync(merchantId, request);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get all webhooks for the current merchant
    /// </summary>
    /// <returns>List of merchant webhooks</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<WebhookDto>>>> GetWebhooks()
    {
        var merchantId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _webhookService.GetMerchantWebhooksAsync(merchantId);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get a specific webhook by ID
    /// </summary>
    /// <param name="webhookId">Webhook ID</param>
    /// <returns>Webhook details</returns>
    [HttpGet("{webhookId:guid}")]
    public async Task<ActionResult<ApiResponse<WebhookDto>>> GetWebhook(Guid webhookId)
    {
        var merchantId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _webhookService.GetWebhookAsync(webhookId, merchantId);

        if (!result.Success && result.Message == "Webhook not found")
        {
            return NotFound(result);
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Update a webhook
    /// </summary>
    /// <param name="webhookId">Webhook ID</param>
    /// <param name="request">Updated webhook configuration</param>
    /// <returns>Updated webhook details</returns>
    [HttpPut("{webhookId:guid}")]
    public async Task<ActionResult<ApiResponse<WebhookDto>>> UpdateWebhook(Guid webhookId, [FromBody] UpdateWebhookRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(ApiResponse<WebhookDto>.ErrorResponse(errors));
        }

        var merchantId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _webhookService.UpdateWebhookAsync(webhookId, merchantId, request);

        if (!result.Success && result.Message == "Webhook not found")
        {
            return NotFound(result);
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Delete a webhook
    /// </summary>
    /// <param name="webhookId">Webhook ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{webhookId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteWebhook(Guid webhookId)
    {
        var merchantId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _webhookService.DeleteWebhookAsync(webhookId, merchantId);

        if (!result.Success && result.Message == "Webhook not found")
        {
            return NotFound(result);
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get available webhook event types
    /// </summary>
    /// <returns>List of available event types</returns>
    [HttpGet("event-types")]
    public ActionResult<ApiResponse<string[]>> GetEventTypes()
    {
        return Ok(ApiResponse<string[]>.SuccessResponse(WebhookEventTypes.AllEventTypes));
    }
}
