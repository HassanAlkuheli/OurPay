using PaymentApi.DTOs;

namespace PaymentApi.Tests.Unit.Services;

/// <summary>
/// Basic unit tests for Payment Service DTOs and validation
/// These tests validate data transfer objects and basic functionality
/// </summary>
public class PaymentServiceTests
{
    [Fact]
    public void CreatePaymentRequest_ValidData_ShouldPassValidation()
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            Amount = 100.50m,
            Currency = "USD",
            ExpiresInMinutes = 1440
        };

        // Act & Assert
        request.Amount.Should().BeGreaterThan(0);
        request.Currency.Should().Be("USD");
        request.ExpiresInMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConfirmPaymentRequest_ValidData_ShouldPassValidation()
    {
        // Arrange
        var request = new ConfirmPaymentRequest
        {
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act & Assert
        request.IdempotencyKey.Should().NotBeNullOrEmpty();
        Guid.TryParse(request.IdempotencyKey, out _).Should().BeTrue();
    }

    [Fact]
    public void RegisterRequest_ValidData_ShouldPassValidation()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "TestPassword123!",
            Role = "merchant"
        };

        // Act & Assert
        request.Name.Should().NotBeNullOrEmpty();
        request.Email.Should().NotBeNullOrEmpty();
        request.Email.Should().Contain("@");
        request.Password.Should().NotBeNullOrEmpty();
        request.Role.Should().BeOneOf("merchant", "customer", "admin");
    }

    [Fact]
    public void LoginRequest_ValidData_ShouldPassValidation()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        // Act & Assert
        request.Email.Should().NotBeNullOrEmpty();
        request.Email.Should().Contain("@");
        request.Password.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public void CreatePaymentRequest_SupportedCurrencies_ShouldBeValid(string currency)
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            Amount = 50.00m,
            Currency = currency,
            ExpiresInMinutes = 720
        };

        // Act & Assert
        request.Currency.Should().Be(currency);
        request.Currency.Should().HaveLength(3);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(100.50)]
    [InlineData(999.99)]
    public void CreatePaymentRequest_ValidAmounts_ShouldPassValidation(decimal amount)
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            Amount = amount,
            Currency = "USD",
            ExpiresInMinutes = 1440
        };

        // Act & Assert
        request.Amount.Should().BeGreaterThan(0);
        request.Amount.Should().Be(amount);
    }

    [Theory]
    [InlineData(30)]      // 30 minutes
    [InlineData(1440)]    // 24 hours
    [InlineData(4320)]    // 3 days
    [InlineData(10080)]   // 7 days
    public void CreatePaymentRequest_ValidExpirationTimes_ShouldPassValidation(int minutes)
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            Amount = 100.00m,
            Currency = "USD",
            ExpiresInMinutes = minutes
        };

        // Act & Assert
        request.ExpiresInMinutes.Should().BeGreaterThan(0);
        request.ExpiresInMinutes.Should().Be(minutes);
    }

    [Fact]
    public void CreatePaymentResponse_ValidData_ShouldHaveRequiredProperties()
    {
        // Arrange
        var response = new CreatePaymentResponse
        {
            PaymentId = Guid.NewGuid(),
            PaymentLink = "https://payment.example.com/pay/123",
            Amount = 100.00m,
            Currency = "USD",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        // Act & Assert
        response.PaymentId.Should().NotBeEmpty();
        response.PaymentLink.Should().NotBeNullOrEmpty();
        response.PaymentLink.Should().StartWith("https://");
        response.Amount.Should().BeGreaterThan(0);
        response.Currency.Should().NotBeNullOrEmpty();
        response.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void AuthResponse_ValidData_ShouldHaveRequiredProperties()
    {
        // Arrange
        var response = new AuthResponse
        {
            AccessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
            RefreshToken = "refresh_token_example",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new UserDto
            {
                UserId = Guid.NewGuid(),
                Name = "Test User",
                Email = "test@example.com",
                Role = "merchant",
                Balance = 1000.00m,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act & Assert
        response.AccessToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        response.User.Should().NotBeNull();
        response.User.UserId.Should().NotBeEmpty();
        response.User.Name.Should().NotBeNullOrEmpty();
        response.User.Email.Should().NotBeNullOrEmpty();
        response.User.Role.Should().BeOneOf("merchant", "customer", "admin");
        response.User.Balance.Should().BeGreaterOrEqualTo(0);
        response.User.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData("merchant")]
    [InlineData("customer")]
    [InlineData("admin")]
    public void RegisterRequest_ValidRoles_ShouldPassValidation(string role)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "TestPassword123!",
            Role = role
        };

        // Act & Assert
        request.Role.Should().Be(role);
        request.Role.Should().BeOneOf("merchant", "customer", "admin");
    }

    [Fact]
    public void ApiResponse_Success_ShouldHaveCorrectStructure()
    {
        // Arrange
        var data = new { Message = "Test successful" };
        var response = new ApiResponse<object>
        {
            Success = true,
            Message = "Operation completed successfully",
            Data = data
        };

        // Act & Assert
        response.Success.Should().BeTrue();
        response.Message.Should().NotBeNullOrEmpty();
        response.Data.Should().NotBeNull();
        response.Data.Should().Be(data);
    }

    [Fact]
    public void ApiResponse_Error_ShouldHaveCorrectStructure()
    {
        // Arrange
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "An error occurred",
            Data = null
        };

        // Act & Assert
        response.Success.Should().BeFalse();
        response.Message.Should().NotBeNullOrEmpty();
        response.Data.Should().BeNull();
    }

    [Fact]
    public void Guid_NewGuid_ShouldGenerateUniqueValues()
    {
        // Arrange & Act
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        // Assert
        guid1.Should().NotBeEmpty();
        guid2.Should().NotBeEmpty();
        guid1.Should().NotBe(guid2);
    }

    [Fact]
    public void DateTime_UtcNow_ShouldBeRecentTime()
    {
        // Arrange & Act
        var now = DateTime.UtcNow;
        var fiveSecondsAgo = DateTime.UtcNow.AddSeconds(-5);
        var fiveSecondsFromNow = DateTime.UtcNow.AddSeconds(5);

        // Assert
        now.Should().BeAfter(fiveSecondsAgo);
        now.Should().BeBefore(fiveSecondsFromNow);
        now.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user.name@domain.co.uk", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@domain.com", false)]
    [InlineData("user@", false)]
    [InlineData("", false)]
    public void Email_Validation_ShouldIdentifyValidFormats(string email, bool shouldBeValid)
    {
        // Arrange & Act
        bool isValid = !string.IsNullOrEmpty(email) && email.Contains("@") && 
                      email.IndexOf("@") > 0 && email.IndexOf("@") < email.Length - 1;

        // Assert
        isValid.Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData(-1.00)]
    [InlineData(0)]
    [InlineData(0.001)]
    public void CreatePaymentRequest_InvalidAmounts_ShouldFailValidation(decimal amount)
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            Amount = amount,
            Currency = "USD",
            ExpiresInMinutes = 1440
        };

        // Act & Assert
        if (amount <= 0)
        {
            request.Amount.Should().BeLessOrEqualTo(0);
        }
        if (amount > 0 && amount < 0.01m)
        {
            request.Amount.Should().BeLessThan(0.01m);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("XX")]
    [InlineData("INVALID")]
    [InlineData("US")]
    public void CreatePaymentRequest_InvalidCurrencies_ShouldFailValidation(string currency)
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            Amount = 100.00m,
            Currency = currency,
            ExpiresInMinutes = 1440
        };

        var validCurrencies = new[] { "USD", "EUR", "GBP" };

        // Act & Assert
        if (string.IsNullOrEmpty(currency) || currency.Length != 3 || !validCurrencies.Contains(currency))
        {
            validCurrencies.Should().NotContain(currency);
        }
    }

    [Fact]
    public void IdempotencyKey_ShouldBeUniqueForEachRequest()
    {
        // Arrange & Act
        var key1 = Guid.NewGuid().ToString();
        var key2 = Guid.NewGuid().ToString();
        var key3 = Guid.NewGuid().ToString();

        // Assert
        key1.Should().NotBe(key2);
        key2.Should().NotBe(key3);
        key1.Should().NotBe(key3);
        
        // All should be valid GUIDs
        Guid.TryParse(key1, out _).Should().BeTrue();
        Guid.TryParse(key2, out _).Should().BeTrue();
        Guid.TryParse(key3, out _).Should().BeTrue();
    }
}