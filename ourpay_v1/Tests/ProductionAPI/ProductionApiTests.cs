using System.Net.Http.Json;
using System.Text.Json;
using PaymentApi.DTOs;
using Xunit;
using FluentAssertions;

namespace PaymentApi.Tests.ProductionAPI;

/// <summary>
/// Production API Tests - Tests actual running API endpoints
/// These tests make real HTTP calls to the API running at localhost:5262
/// </summary>
public class ProductionApiTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://localhost:5262";

    public ProductionApiTests()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PaymentAPI-Tests/1.0");
    }

    [Fact]
    public async Task API_IsRunning_ShouldReturnResponse()
    {
        // Test if API is running by hitting any endpoint (even if it fails)
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/auth/test-endpoint");
            // As long as we get a response (even 404), the API is running
            response.Should().NotBeNull();
        }
        catch (HttpRequestException)
        {
            Assert.Fail("API is not running at localhost:5262");
        }
    }

    [Fact]
    public async Task API_InvalidEndpoint_ShouldReturn404()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/v1/invalid-endpoint");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Payments_RequiresAuthentication()
    {
        // Arrange
        var createPaymentRequest = new CreatePaymentRequest
        {
            Amount = 100.00m,
            Currency = "USD",
            ExpiresInMinutes = 1440
        };

        // Act - Try to create payment without authentication
        var response = await _httpClient.PostAsJsonAsync("/api/v1/payments", createPaymentRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
