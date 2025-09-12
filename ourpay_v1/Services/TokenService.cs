using Microsoft.IdentityModel.Tokens;
using PaymentApi.Configuration;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PaymentApi.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ICacheService _cacheService;
    private readonly ILogger<TokenService> _logger;

    public TokenService(JwtSettings jwtSettings, ICacheService cacheService, ILogger<TokenService> logger)
    {
        _jwtSettings = jwtSettings;
        _cacheService = cacheService;
        _logger = logger;
    }

    public string GenerateAccessToken(Guid userId, string email, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[64];
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
    {
        try
        {
            return await _cacheService.ExistsAsync($"refresh_token:{refreshToken}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token");
            return false;
        }
    }

    public async Task StoreRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAt)
    {
        try
        {
            var expiration = expiresAt - DateTime.UtcNow;
            await _cacheService.SetAsync($"refresh_token:{refreshToken}", userId.ToString(), expiration);
            
            // Also store by user ID for token revocation
            await _cacheService.SetAsync($"user_refresh_token:{userId}", refreshToken, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing refresh token for user {UserId}", userId);
            throw;
        }
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var userIdString = await _cacheService.GetAsync<string>($"refresh_token:{refreshToken}");
            if (userIdString != null && Guid.TryParse(userIdString, out var userId))
            {
                await _cacheService.RemoveAsync($"user_refresh_token:{userId}");
            }
            
            await _cacheService.RemoveAsync($"refresh_token:{refreshToken}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            throw;
        }
    }
}
