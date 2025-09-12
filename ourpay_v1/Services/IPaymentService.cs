using PaymentApi.DTOs;

namespace PaymentApi.Services;

public interface IPaymentService
{
    Task<ApiResponse<CreatePaymentResponse>> CreatePaymentAsync(Guid merchantId, CreatePaymentRequest request);
    Task<ApiResponse<PaymentDto>> GetPaymentAsync(Guid paymentId);
    Task<ApiResponse<ConfirmPaymentResponse>> ConfirmPaymentAsync(Guid paymentId, Guid customerId, ConfirmPaymentRequest request);
    Task<ApiResponse<bool>> CancelPaymentAsync(Guid paymentId, Guid userId, string userRole);
    Task<ApiResponse<PaymentListResponse>> GetPaymentsAsync(Guid? merchantId, string userRole, int page = 1, int pageSize = 10);
    Task ProcessExpiredPaymentsAsync();
    string GeneratePaymentLink(Guid paymentId);
}
