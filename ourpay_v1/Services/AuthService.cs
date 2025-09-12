using AutoMapper;
using Microsoft.AspNetCore.Identity;
using PaymentApi.DTOs;
using PaymentApi.Models;
using PaymentApi.Repositories;

namespace PaymentApi.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ITokenService tokenService,
        IAuditService auditService,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _auditService = auditService;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Validate role
            if (!Enum.TryParse<UserRole>(request.Role, true, out var userRole))
            {
                return ApiResponse<AuthResponse>.ErrorResponse("Invalid role specified");
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return ApiResponse<AuthResponse>.ErrorResponse("User with this email already exists");
            }

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                Name = request.Name,
                Role = userRole,
                Balance = userRole == UserRole.Customer ? 1000.00m : 0.00m, // Give customers initial balance
                EmailConfirmed = true // For demo purposes
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return ApiResponse<AuthResponse>.ErrorResponse("User registration failed", errors);
            }

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.Role.ToString());
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            await _tokenService.StoreRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry);

            // Log audit
            await _auditService.LogActionAsync(user.Id, "user_register", new { Role = user.Role.ToString() });

            var authResponse = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                User = _mapper.Map<UserDto>(user)
            };

            _logger.LogInformation("User {Email} registered successfully as {Role}", request.Email, userRole);
            return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "User registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Email}", request.Email);
            return ApiResponse<AuthResponse>.ErrorResponse("An error occurred during registration");
        }
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return ApiResponse<AuthResponse>.ErrorResponse("Invalid email or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                await _auditService.LogActionAsync(user.Id, "login_failed", new { Reason = "Invalid password" });
                return ApiResponse<AuthResponse>.ErrorResponse("Invalid email or password");
            }

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.Role.ToString());
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            await _tokenService.StoreRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry);

            // Log audit
            await _auditService.LogActionAsync(user.Id, "user_login");

            var authResponse = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                User = _mapper.Map<UserDto>(user)
            };

            _logger.LogInformation("User {Email} logged in successfully", request.Email);
            return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user {Email}", request.Email);
            return ApiResponse<AuthResponse>.ErrorResponse("An error occurred during login");
        }
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            if (!await _tokenService.ValidateRefreshTokenAsync(request.RefreshToken))
            {
                return ApiResponse<AuthResponse>.ErrorResponse("Invalid refresh token");
            }

            // Get user ID from cached refresh token
            var userIdString = await _cacheService.GetAsync<string>($"refresh_token:{request.RefreshToken}");
            if (userIdString == null || !Guid.TryParse(userIdString, out var userId))
            {
                return ApiResponse<AuthResponse>.ErrorResponse("Invalid refresh token");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse<AuthResponse>.ErrorResponse("User not found");
            }

            // Generate new tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.Role.ToString());
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // Revoke old refresh token and store new one
            await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken);
            await _tokenService.StoreRefreshTokenAsync(user.Id, newRefreshToken, refreshTokenExpiry);

            var authResponse = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                User = _mapper.Map<UserDto>(user)
            };

            return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return ApiResponse<AuthResponse>.ErrorResponse("An error occurred during token refresh");
        }
    }

    public async Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken)
    {
        try
        {
            await _tokenService.RevokeRefreshTokenAsync(refreshToken);
            return ApiResponse<bool>.SuccessResponse(true, "Token revoked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return ApiResponse<bool>.ErrorResponse("An error occurred during token revocation");
        }
    }
}
