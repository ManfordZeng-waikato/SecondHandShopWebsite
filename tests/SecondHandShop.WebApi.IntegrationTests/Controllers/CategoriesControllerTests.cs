using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using SecondHandShop.Application.UseCases.Categories;
using SecondHandShop.Application.UseCases.Categories.CreateCategory;
using SecondHandShop.Application.UseCases.Categories.GetCategoryTree;
using SecondHandShop.Domain.Entities;
using SecondHandShop.WebApi.Controllers;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class CategoriesControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public CategoriesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task List_ShouldReturnActiveCategories()
    {
        var category = Category.Create("Bags", "bags", null, 1, true, null, new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc));
        _factory.CategoryRepositoryMock
            .Setup(x => x.ListActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([category]);

        using var client = CreateClient();

        var response = await client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(1);
        body[0].Slug.Should().Be("bags");
    }

    [Fact]
    public async Task GetTree_ShouldReturnTreeStructure()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        _factory.MediatorMock
            .Setup(x => x.Send(
                It.IsAny<GetCategoryTreeQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryDto>
            {
                new(rootId, "Bags", "bags", [new CategoryDto(childId, "Totes", "totes", [])])
            });

        using var client = CreateClient();

        var response = await client.GetAsync("/api/categories/tree");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CategoryTreeItemResponse>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(1);
        body[0].Children.Should().HaveCount(1);
        body[0].Children[0].Slug.Should().Be("totes");
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenAdminIsAuthenticated()
    {
        var categoryId = Guid.NewGuid();
        _factory.MediatorMock
            .Setup(x => x.Send(
                It.Is<CreateCategoryCommand>(command =>
                    command.Name == "Bags"
                    && command.Slug == "bags"
                    && command.SortOrder == 2
                    && command.IsActive
                    && command.CreatedByAdminUserId == _factory.ActiveAdminId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CategoryDto(categoryId, "Bags", "bags", []));

        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/categories", new CreateCategoryApiRequest("Bags", "bags", null, 2, true));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Be($"/api/categories/{categoryId}");
        var body = await response.Content.ReadFromJsonAsync<CreateCategoryResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(categoryId);
    }

    private HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(
            "Cookie",
            TestWebApplicationFactory.CreateCookieHeader(_factory.CreateAdminToken()));
        return client;
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }
}
