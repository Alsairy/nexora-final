using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexora.Core.Data;
using Nexora.Core.Entities;
using Nexora.Web.Api;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Nexora.Tests.Integration;

public class PaymentIntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly HttpClient _client;

    public PaymentIntegrationTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<NexoraDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<NexoraDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<NexoraDbContext>();
                var logger = scopedServices.GetRequiredService<ILogger<PaymentIntegrationTests>>();

                db.Database.EnsureCreated();

                try
                {
                    SeedTestData(db);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the database with test messages. Error: {Message}", ex.Message);
                }
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreatePayment_ValidPayment_ReturnsSuccess()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var paymentRequest = new
        {
            Amount = 100.00m,
            Currency = "USD",
            Provider = "stripe",
            Description = "Test payment",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var response = await _client.PostAsJsonAsync("/api/v1/payments", paymentRequest);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var paymentResponse = JsonSerializer.Deserialize<PaymentResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(paymentResponse);
        Assert.Equal(100.00m, paymentResponse.Amount);
        Assert.Equal("USD", paymentResponse.Currency);
        Assert.Equal("stripe", paymentResponse.Provider);
    }

    [Fact]
    public async Task CreatePayment_HighValuePayment_RequiresAdditionalAuth()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var paymentRequest = new
        {
            Amount = 15000.00m, // High value payment
            Currency = "USD",
            Provider = "stripe",
            Description = "High value test payment",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var response = await _client.PostAsJsonAsync("/api/v1/payments", paymentRequest);

        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var paymentResponse = JsonSerializer.Deserialize<PaymentResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(paymentResponse);
        Assert.Equal("PendingAuth", paymentResponse.Status);
    }

    [Fact]
    public async Task CreatePayment_DuplicateIdempotencyKey_ReturnsSamePayment()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var idempotencyKey = Guid.NewGuid().ToString();
        var paymentRequest = new
        {
            Amount = 50.00m,
            Currency = "USD",
            Provider = "stripe",
            Description = "Idempotency test payment",
            IdempotencyKey = idempotencyKey
        };

        var response1 = await _client.PostAsJsonAsync("/api/v1/payments", paymentRequest);
        response1.EnsureSuccessStatusCode();
        var content1 = await response1.Content.ReadAsStringAsync();
        var payment1 = JsonSerializer.Deserialize<PaymentResponse>(content1, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var response2 = await _client.PostAsJsonAsync("/api/v1/payments", paymentRequest);
        response2.EnsureSuccessStatusCode();
        var content2 = await response2.Content.ReadAsStringAsync();
        var payment2 = JsonSerializer.Deserialize<PaymentResponse>(content2, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(payment1);
        Assert.NotNull(payment2);
        Assert.Equal(payment1.Id, payment2.Id);
        Assert.Equal(payment1.Amount, payment2.Amount);
    }

    [Fact]
    public async Task GetPayment_ExistingPayment_ReturnsPayment()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var paymentRequest = new
        {
            Amount = 75.00m,
            Currency = "USD",
            Provider = "stripe",
            Description = "Get payment test",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/payments", paymentRequest);
        createResponse.EnsureSuccessStatusCode();
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPayment = JsonSerializer.Deserialize<PaymentResponse>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var getResponse = await _client.GetAsync($"/api/v1/payments/{createdPayment!.Id}");

        getResponse.EnsureSuccessStatusCode();
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var retrievedPayment = JsonSerializer.Deserialize<PaymentResponse>(getContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(retrievedPayment);
        Assert.Equal(createdPayment.Id, retrievedPayment.Id);
        Assert.Equal(75.00m, retrievedPayment.Amount);
    }

    [Fact]
    public async Task GetPayment_NonExistentPayment_ReturnsNotFound()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var nonExistentId = Guid.NewGuid().ToString();

        var response = await _client.GetAsync($"/api/v1/payments/{nonExistentId}");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserPayments_ValidUser_ReturnsPayments()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        for (int i = 0; i < 3; i++)
        {
            var paymentRequest = new
            {
                Amount = 25.00m * (i + 1),
                Currency = "USD",
                Provider = "stripe",
                Description = $"User payments test {i + 1}",
                IdempotencyKey = Guid.NewGuid().ToString()
            };

            var createResponse = await _client.PostAsJsonAsync("/api/v1/payments", paymentRequest);
            createResponse.EnsureSuccessStatusCode();
        }

        var response = await _client.GetAsync("/api/v1/payments/user");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var payments = JsonSerializer.Deserialize<List<PaymentResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(payments);
        Assert.True(payments.Count >= 3);
    }

    [Fact]
    public async Task CreatePayment_InvalidCurrency_ReturnsBadRequest()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var paymentRequest = new
        {
            Amount = 100.00m,
            Currency = "INVALID",
            Provider = "stripe",
            Description = "Invalid currency test",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var response = await _client.PostAsJsonAsync("/api/v1/payments", paymentRequest);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_NegativeAmount_ReturnsBadRequest()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var paymentRequest = new
        {
            Amount = -50.00m,
            Currency = "USD",
            Provider = "stripe",
            Description = "Negative amount test",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var response = await _client.PostAsJsonAsync("/api/v1/payments", paymentRequest);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WithoutAuth_ReturnsUnauthorized()
    {
        var paymentRequest = new
        {
            Amount = 100.00m,
            Currency = "USD",
            Provider = "stripe",
            Description = "Unauthorized test",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var response = await _client.PostAsJsonAsync("/api/v1/payments", paymentRequest);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PaymentEndpoints_RateLimiting_EnforcesLimits()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < 150; i++) // Exceed the rate limit
        {
            var paymentRequest = new
            {
                Amount = 1.00m,
                Currency = "USD",
                Provider = "stripe",
                Description = $"Rate limit test {i}",
                IdempotencyKey = Guid.NewGuid().ToString()
            };

            tasks.Add(_client.PostAsJsonAsync("/api/v1/payments", paymentRequest));
        }

        var responses = await Task.WhenAll(tasks);

        var rateLimitedResponses = responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
        Assert.True(rateLimitedResponses > 0, "Rate limiting should have been triggered");
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var loginRequest = new
        {
            Email = "test@nexora.com",
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return loginResponse!.Token;
    }

    private static void SeedTestData(NexoraDbContext context)
    {
        var tenant = new Tenant
        {
            Id = "test-tenant",
            Name = "Test Tenant",
            Subdomain = "test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Tenants.Add(tenant);

        var user = new User
        {
            Id = "test-user",
            Email = "test@nexora.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            TenantId = "test-tenant",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        context.SaveChanges();
    }
}

public class PaymentResponse
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
