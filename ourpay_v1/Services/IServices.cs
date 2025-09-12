using PaymentApi.Models;
using PaymentApi.DTOs;

namespace PaymentApi.Services;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken);
}

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateRefreshToken();
    Task<bool> ValidateRefreshTokenAsync(string refreshToken);
    Task StoreRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAt);
    Task RevokeRefreshTokenAsync(string refreshToken);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task IncrementAsync(string key, TimeSpan? expiration = null);
    Task<long> GetCounterAsync(string key);
}

public interface IAuditService
{
    Task LogActionAsync(Guid userId, string action, object? details = null, Guid? paymentId = null);
    Task<IEnumerable<AuditLog>> GetPaymentLogsAsync(Guid paymentId);
}
