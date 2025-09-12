using AutoMapper;
using PaymentApi.Configuration;
using PaymentApi.DTOs;
using PaymentApi.Models;
using PaymentApi.Repositories;

namespace PaymentApi.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IAuditService _auditService;
    private readonly IWebhookService _webhookService;
    private readonly IMapper _mapper;
    private readonly PaymentSettings _paymentSettings;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IAuditService auditService,
        IWebhookService webhookService,
        IMapper mapper,
        PaymentSettings paymentSettings,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _auditService = auditService;
        _webhookService = webhookService;
        _mapper = mapper;
        _paymentSettings = paymentSettings;
        _logger = logger;
    }

    public async Task<ApiResponse<CreatePaymentResponse>> CreatePaymentAsync(Guid merchantId, CreatePaymentRequest request)
    {
        try
        {
            // Validate merchant exists
            if (!await _unitOfWork.Users.ExistsAsync(merchantId))
            {
                return ApiResponse<CreatePaymentResponse>.ErrorResponse("Merchant not found");
            }

            // Validate currency
            if (!_paymentSettings.SupportedCurrencies.Contains(request.Currency.ToUpper()))
            {
                return ApiResponse<CreatePaymentResponse>.ErrorResponse($"Currency {request.Currency} is not supported");
            }

            // Validate amount
            if (request.Amount < _paymentSettings.MinAmount || request.Amount > _paymentSettings.MaxAmount)
            {
                return ApiResponse<CreatePaymentResponse>.ErrorResponse(
                    $"Amount must be between {_paymentSettings.MinAmount} and {_paymentSettings.MaxAmount}");
            }

            // Validate expiration time
            if (request.ExpiresInMinutes > _paymentSettings.MaxExpirationMinutes)
            {
                return ApiResponse<CreatePaymentResponse>.ErrorResponse(
                    $"Expiration time cannot exceed {_paymentSettings.MaxExpirationMinutes} minutes");
            }

            var payment = _mapper.Map<Payment>(request);
            payment.MerchantId = merchantId;
            payment.ExpiresAt = DateTime.UtcNow.AddMinutes(request.ExpiresInMinutes);

            await _unitOfWork.Payments.CreateAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            // Log audit
            await _auditService.LogActionAsync(merchantId, "payment_create", 
                new { PaymentId = payment.PaymentId, Amount = payment.Amount, Currency = payment.Currency }, 
                payment.PaymentId);

            var response = _mapper.Map<CreatePaymentResponse>(payment);
            response.PaymentLink = GeneratePaymentLink(payment.PaymentId);

            // Publish webhook event
            await _webhookService.PublishWebhookEventAsync(
                WebhookEventTypes.PaymentCreated,
                payment.PaymentId,
                merchantId,
                new { 
                    paymentId = payment.PaymentId,
                    amount = payment.Amount,
                    currency = payment.Currency,
                    status = payment.Status.ToString(),
                    expiresAt = payment.ExpiresAt,
                    paymentLink = response.PaymentLink
                });

            _logger.LogInformation("Payment {PaymentId} created by merchant {MerchantId}", 
                payment.PaymentId, merchantId);

            return ApiResponse<CreatePaymentResponse>.SuccessResponse(response, "Payment created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for merchant {MerchantId}", merchantId);
            return ApiResponse<CreatePaymentResponse>.ErrorResponse("An error occurred while creating the payment");
        }
    }

    public async Task<ApiResponse<PaymentDto>> GetPaymentAsync(Guid paymentId)
    {
        try
        {
            var payment = await _unitOfWork.Payments.GetByIdWithDetailsAsync(paymentId);
            if (payment == null)
            {
                return ApiResponse<PaymentDto>.ErrorResponse("Payment not found");
            }

            // Check if payment is expired and update status
            if (payment.Status == PaymentStatus.Pending && payment.ExpiresAt <= DateTime.UtcNow)
            {
                payment.Status = PaymentStatus.Expired;
                await _unitOfWork.Payments.UpdateAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                await _auditService.LogActionAsync(payment.MerchantId, "payment_expired", 
                    new { Reason = "Automatic expiration" }, paymentId);

                // Publish webhook event
                await _webhookService.PublishWebhookEventAsync(
                    WebhookEventTypes.PaymentExpired,
                    payment.PaymentId,
                    payment.MerchantId,
                    new { 
                        paymentId = payment.PaymentId,
                        amount = payment.Amount,
                        currency = payment.Currency,
                        status = payment.Status.ToString(),
                        expiredAt = DateTime.UtcNow,
                        reason = "Automatic expiration"
                    });
            }

            var paymentDto = _mapper.Map<PaymentDto>(payment);
            return ApiResponse<PaymentDto>.SuccessResponse(paymentDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment {PaymentId}", paymentId);
            return ApiResponse<PaymentDto>.ErrorResponse("An error occurred while retrieving the payment");
        }
    }

    public async Task<ApiResponse<ConfirmPaymentResponse>> ConfirmPaymentAsync(Guid paymentId, Guid customerId, ConfirmPaymentRequest request)
    {
        try
        {
            // Check idempotency
            var idempotencyKey = $"payment_confirm:{paymentId}:{request.IdempotencyKey}";
            if (await _cacheService.ExistsAsync(idempotencyKey))
            {
                var cachedResponse = await _cacheService.GetAsync<ConfirmPaymentResponse>(idempotencyKey);
                if (cachedResponse != null)
                {
                    return ApiResponse<ConfirmPaymentResponse>.SuccessResponse(cachedResponse, "Payment already processed");
                }
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var payment = await _unitOfWork.Payments.GetByIdWithDetailsAsync(paymentId);
                if (payment == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ApiResponse<ConfirmPaymentResponse>.ErrorResponse("Payment not found");
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ApiResponse<ConfirmPaymentResponse>.ErrorResponse($"Payment cannot be confirmed. Current status: {payment.Status}");
                }

                if (payment.ExpiresAt <= DateTime.UtcNow)
                {
                    payment.Status = PaymentStatus.Expired;
                    await _unitOfWork.Payments.UpdateAsync(payment);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.RollbackTransactionAsync();
                    
                    await _auditService.LogActionAsync(customerId, "payment_confirm_failed", 
                        new { Reason = "Payment expired" }, paymentId);
                    
                    return ApiResponse<ConfirmPaymentResponse>.ErrorResponse("Payment has expired");
                }

                // Get customer
                var customer = await _unitOfWork.Users.GetByIdAsync(customerId);
                if (customer == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ApiResponse<ConfirmPaymentResponse>.ErrorResponse("Customer not found");
                }

                // Check customer balance
                if (customer.Balance < payment.Amount)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    await _auditService.LogActionAsync(customerId, "payment_confirm_failed", 
                        new { Reason = "Insufficient balance", RequiredAmount = payment.Amount, CurrentBalance = customer.Balance }, 
                        paymentId);
                    
                    return ApiResponse<ConfirmPaymentResponse>.ErrorResponse("Insufficient balance");
                }

                // Get merchant
                var merchant = await _unitOfWork.Users.GetByIdAsync(payment.MerchantId);
                if (merchant == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ApiResponse<ConfirmPaymentResponse>.ErrorResponse("Merchant not found");
                }

                // Process payment - deduct from customer, add to merchant
                await _unitOfWork.Users.UpdateBalanceAsync(customerId, customer.Balance - payment.Amount);
                await _unitOfWork.Users.UpdateBalanceAsync(payment.MerchantId, merchant.Balance + payment.Amount);

                // Update payment
                payment.CustomerId = customerId;
                payment.Status = PaymentStatus.Success;
                await _unitOfWork.Payments.UpdateAsync(payment);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Log audit
                await _auditService.LogActionAsync(customerId, "payment_confirm", 
                    new { Amount = payment.Amount, MerchantId = payment.MerchantId }, paymentId);

                var response = _mapper.Map<ConfirmPaymentResponse>(payment);

                // Store in cache for idempotency
                await _cacheService.SetAsync(idempotencyKey, response, TimeSpan.FromHours(24));

                // Publish webhook event
                await _webhookService.PublishWebhookEventAsync(
                    WebhookEventTypes.PaymentConfirmed,
                    payment.PaymentId,
                    payment.MerchantId,
                    new { 
                        paymentId = payment.PaymentId,
                        customerId = customerId,
                        amount = payment.Amount,
                        currency = payment.Currency,
                        status = payment.Status.ToString(),
                        processedAt = DateTime.UtcNow
                    });

                _logger.LogInformation("Payment {PaymentId} confirmed by customer {CustomerId}", paymentId, customerId);

                return ApiResponse<ConfirmPaymentResponse>.SuccessResponse(response, "Payment confirmed successfully");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment {PaymentId} for customer {CustomerId}", paymentId, customerId);
            return ApiResponse<ConfirmPaymentResponse>.ErrorResponse("An error occurred while confirming the payment");
        }
    }

    public async Task<ApiResponse<bool>> CancelPaymentAsync(Guid paymentId, Guid userId, string userRole)
    {
        try
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
            if (payment == null)
            {
                return ApiResponse<bool>.ErrorResponse("Payment not found");
            }

            // Check permissions
            if (userRole != "Admin" && payment.MerchantId != userId)
            {
                return ApiResponse<bool>.ErrorResponse("You don't have permission to cancel this payment");
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                return ApiResponse<bool>.ErrorResponse($"Payment cannot be cancelled. Current status: {payment.Status}");
            }

            payment.Status = PaymentStatus.Cancelled;
            await _unitOfWork.Payments.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            // Log audit
            await _auditService.LogActionAsync(userId, "payment_cancel", 
                new { CancelledBy = userRole }, paymentId);

            // Publish webhook event
            await _webhookService.PublishWebhookEventAsync(
                WebhookEventTypes.PaymentCancelled,
                payment.PaymentId,
                payment.MerchantId,
                new { 
                    paymentId = payment.PaymentId,
                    cancelledBy = userRole,
                    cancelledByUserId = userId,
                    amount = payment.Amount,
                    currency = payment.Currency,
                    status = payment.Status.ToString(),
                    cancelledAt = DateTime.UtcNow
                });

            _logger.LogInformation("Payment {PaymentId} cancelled by user {UserId} ({Role})", 
                paymentId, userId, userRole);

            return ApiResponse<bool>.SuccessResponse(true, "Payment cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment {PaymentId}", paymentId);
            return ApiResponse<bool>.ErrorResponse("An error occurred while cancelling the payment");
        }
    }

    public async Task<ApiResponse<PaymentListResponse>> GetPaymentsAsync(Guid? merchantId, string userRole, int page = 1, int pageSize = 10)
    {
        try
        {
            IEnumerable<Payment> payments;
            int totalCount;

            if (userRole == "Admin")
            {
                payments = await _unitOfWork.Payments.GetAllAsync(page, pageSize);
                totalCount = await _unitOfWork.Payments.GetTotalCountAsync();
            }
            else if (userRole == "Merchant" && merchantId.HasValue)
            {
                payments = await _unitOfWork.Payments.GetByMerchantIdAsync(merchantId.Value, page, pageSize);
                totalCount = await _unitOfWork.Payments.GetTotalCountAsync(merchantId.Value);
            }
            else
            {
                return ApiResponse<PaymentListResponse>.ErrorResponse("Unauthorized to view payments");
            }

            var paymentDtos = _mapper.Map<IEnumerable<PaymentDto>>(payments);
            var response = new PaymentListResponse
            {
                Payments = paymentDtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return ApiResponse<PaymentListResponse>.SuccessResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for user {UserId} ({Role})", merchantId, userRole);
            return ApiResponse<PaymentListResponse>.ErrorResponse("An error occurred while retrieving payments");
        }
    }

    public async Task ProcessExpiredPaymentsAsync()
    {
        try
        {
            var expiredPayments = await _unitOfWork.Payments.GetExpiredPaymentsAsync();
            
            foreach (var payment in expiredPayments)
            {
                payment.Status = PaymentStatus.Expired;
                await _unitOfWork.Payments.UpdateAsync(payment);
                
                await _auditService.LogActionAsync(payment.MerchantId, "payment_expired", 
                    new { Reason = "Automatic cleanup" }, payment.PaymentId);
            }

            if (expiredPayments.Any())
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Processed {Count} expired payments", expiredPayments.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired payments");
        }
    }

    public string GeneratePaymentLink(Guid paymentId)
    {
        return $"{_paymentSettings.BaseUrl}/pay/{paymentId}";
    }
}
