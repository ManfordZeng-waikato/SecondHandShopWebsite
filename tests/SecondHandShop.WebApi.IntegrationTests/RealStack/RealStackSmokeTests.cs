using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using SecondHandShop.Application.Abstractions.Messaging;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Contracts.Admin;
using SecondHandShop.Application.Contracts.Analytics;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Infrastructure.Persistence;

namespace SecondHandShop.WebApi.IntegrationTests.RealStack;

[Collection("WebApiRealStack")]
public sealed class RealStackSmokeTests : IDisposable
{
    private readonly RealStackPostgresFixture _postgres;
    private readonly RealStackWebApplicationFactory _factory;

    public RealStackSmokeTests(RealStackPostgresFixture postgres)
    {
        _postgres = postgres;
        _postgres.SkipIfUnavailable();

        _factory = new RealStackWebApplicationFactory
        {
            ConnectionString = _postgres.ConnectionString
        };
    }

    [SkippableFact]
    public async Task AdminCatalogFlow_ShouldCreateProduct_AndExposeItInPublicSearch()
    {
        using var client = CreateClient();
        await LoginAsE2EAdminAsync(client);
        var suffix = UniqueSuffix();

        var categoryId = await CreateCategoryAsync(client, $"Real Stack Category {suffix}", $"real-stack-category-{suffix}");
        var productId = await CreateProductAsync(client, categoryId, $"Real Stack Product {suffix}", $"real-stack-product-{suffix}", 149.99m);

        var response = await client.GetAsync($"/api/products/search?search={Uri.EscapeDataString($"Real Stack Product {suffix}")}&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<ProductListItem>>();
        body.Should().NotBeNull();
        body!.Items.Should().Contain(item =>
            item.Id == productId
            && item.Title == $"Real Stack Product {suffix}"
            && item.Slug == $"real-stack-product-{suffix}"
            && item.CategoryName == $"Real Stack Category {suffix}"
            && item.Status == "Available");
    }

    [SkippableFact]
    public async Task InquiryFlow_ShouldPersistInquiry_AndDispatchFakeEmail()
    {
        using var client = CreateClient(("CF-Connecting-IP", $"203.0.113.{Random.Shared.Next(1, 200)}"));
        await LoginAsE2EAdminAsync(client);
        var suffix = UniqueSuffix();

        var categoryId = await CreateCategoryAsync(client, $"Inquiry Category {suffix}", $"inquiry-category-{suffix}");
        var productId = await CreateProductAsync(client, categoryId, $"Inquiry Product {suffix}", $"inquiry-product-{suffix}", 79m);

        var createInquiryResponse = await client.PostAsJsonAsync("/api/inquiries", new
        {
            productId,
            customerName = "Real Stack Buyer",
            email = $"buyer-{suffix}@example.test",
            phoneNumber = "021 123 4567",
            message = $"Please tell me more about product {suffix}.",
            turnstileToken = "fake-turnstile-token"
        });

        createInquiryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createInquiryResponse.Content.ReadFromJsonAsync<CreateInquiryResult>();
        created.Should().NotBeNull();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
            var inquiry = await db.Inquiries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == created!.InquiryId);
            inquiry.Should().NotBeNull();
            inquiry!.ProductId.Should().Be(productId);
            inquiry.Email.Should().Be($"buyer-{suffix}@example.test");
        }

        var email = await WaitForInquiryEmailAsync(created!.InquiryId);
        email.ProductId.Should().Be(productId);
        email.ProductTitle.Should().Be($"Inquiry Product {suffix}");
        email.Email.Should().Be($"buyer-{suffix}@example.test");
    }

    [SkippableFact]
    public async Task MarkAsSoldFlow_ShouldAppearInThirtyDayAnalytics()
    {
        using var client = CreateClient();
        await LoginAsE2EAdminAsync(client);
        var suffix = UniqueSuffix();

        var categoryId = await CreateCategoryAsync(client, $"Sold Category {suffix}", $"sold-category-{suffix}");
        var productId = await CreateProductAsync(client, categoryId, $"Sold Product {suffix}", $"sold-product-{suffix}", 250m);

        var markSoldResponse = await client.PostAsJsonAsync($"/api/lord/products/{productId}/mark-sold", new
        {
            finalSoldPrice = 225m,
            soldAtUtc = DateTime.UtcNow,
            buyerName = "Sold Buyer",
            buyerEmail = $"sold-buyer-{suffix}@example.test",
            paymentMethod = "Cash",
            notes = "Real-stack smoke sale"
        });

        markSoldResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var analyticsResponse = await client.GetAsync("/api/lord/analytics/overview?range=30d");

        analyticsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var analytics = await analyticsResponse.Content.ReadFromJsonAsync<AnalyticsOverviewDto>();
        analytics.Should().NotBeNull();
        analytics!.Summary.TotalSoldItems.Should().BeGreaterThanOrEqualTo(1);
        analytics.Summary.TotalRevenue.Should().BeGreaterThanOrEqualTo(225m);
        analytics.SalesTrend.Should().Contain(point => point.SoldCount >= 1 && point.Revenue >= 225m);
    }

    [SkippableFact]
    public async Task ForcedPasswordChange_ShouldInvalidateOldToken_AndAllowNewFullAccessLogin()
    {
        using var client = CreateClient();
        var suffix = UniqueSuffix();
        var userName = $"forced-admin-{suffix}";
        const string initialPassword = "Initial-Admin-Pa55!";
        const string newPassword = "Changed-Admin-Pa55!";
        await CreateForcedPasswordAdminAsync(userName, initialPassword);

        var loginResponse = await client.PostAsJsonAsync("/api/lord/auth/login", new LoginAdminRequest(userName, initialPassword));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var restrictedCookie = ExtractAdminCookie(loginResponse);

        var deniedFullAccess = await SendWithCookieAsync(client, HttpMethod.Get, "/api/lord/ping", restrictedCookie);
        deniedFullAccess.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var changeResponse = await SendWithCookieAsync(
            client,
            HttpMethod.Post,
            "/api/lord/auth/change-initial-password",
            restrictedCookie,
            new
            {
                currentPassword = initialPassword,
                newPassword,
                confirmNewPassword = newPassword
            });
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var staleTokenResponse = await SendWithCookieAsync(client, HttpMethod.Get, "/api/lord/auth/me", restrictedCookie);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var reloginResponse = await client.PostAsJsonAsync("/api/lord/auth/login", new LoginAdminRequest(userName, newPassword));
        reloginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fullAccessCookie = ExtractAdminCookie(reloginResponse);

        var allowedFullAccess = await SendWithCookieAsync(client, HttpMethod.Get, "/api/lord/ping", fullAccessCookie);
        allowedFullAccess.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private HttpClient CreateClient(params (string Name, string Value)[] headers)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false
        });

        foreach (var (name, value) in headers)
        {
            client.DefaultRequestHeaders.Add(name, value);
        }

        return client;
    }

    private static async Task LoginAsE2EAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/lord/auth/login",
            new LoginAdminRequest(RealStackWebApplicationFactory.AdminUserName, RealStackWebApplicationFactory.AdminPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", ExtractAdminCookie(response));
    }

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string name, string slug)
    {
        var response = await client.PostAsJsonAsync("/api/categories", new
        {
            name,
            slug,
            parentId = (Guid?)null,
            sortOrder = 0,
            isActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedId>();
        body.Should().NotBeNull();
        return body!.Id;
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, Guid categoryId, string title, string slug, decimal price)
    {
        var response = await client.PostAsJsonAsync("/api/lord/products", new
        {
            title,
            slug,
            description = $"Smoke product {slug}",
            price,
            categoryId,
            condition = "Good"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedId>();
        body.Should().NotBeNull();
        return body!.Id;
    }

    private async Task CreateForcedPasswordAdminAsync(string userName, string password)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var db = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        db.AdminUsers.Add(AdminUser.CreateWithCredentials(
            userName,
            "Forced Password Admin",
            hasher.Hash(password),
            "Admin",
            mustChangePassword: true));
        await db.SaveChangesAsync();
    }

    private async Task<InquiryEmailMessage> WaitForInquiryEmailAsync(Guid inquiryId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var message = _factory.EmailSender.InquiryMessages.SingleOrDefault(x => x.InquiryId == inquiryId);
            if (message is not null)
            {
                return message;
            }

            await Task.Delay(100);
        }

        _factory.EmailSender.InquiryMessages.Should().Contain(x => x.InquiryId == inquiryId);
        throw new InvalidOperationException($"Inquiry email '{inquiryId}' was not dispatched.");
    }

    private static async Task<HttpResponseMessage> SendWithCookieAsync(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        string cookie,
        object? jsonBody = null)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("Cookie", cookie);
        if (jsonBody is not null)
        {
            request.Content = JsonContent.Create(jsonBody);
        }

        return await client.SendAsync(request);
    }

    private static string ExtractAdminCookie(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out var values).Should().BeTrue();
        var setCookie = values!.Single(header => header.StartsWith("shs.admin.token=", StringComparison.Ordinal));
        return SetCookieHeaderValue.Parse(setCookie).ToString().Split(';', 2)[0];
    }

    private static string UniqueSuffix()
        => Guid.NewGuid().ToString("N")[..12];

    private sealed record CreatedId(Guid Id);
    private sealed record CreateInquiryResult(Guid InquiryId);
    private sealed record ProductListItem(
        Guid Id,
        string Title,
        string Slug,
        decimal Price,
        string? CoverImageUrl,
        string? CategoryName,
        string Status,
        string? Condition,
        DateTime CreatedAt);
}
