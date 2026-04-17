using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.UseCases.Categories.GetCategoryTree;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.UnitTests.UseCases.Categories;

public class GetCategoryTreeQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldBuildTree_SortedBySortOrderThenName()
    {
        var rootA = Category.Create("Bags", "bags", null, 2, true, null, UtcNow);
        var rootB = Category.Create("Accessories", "accessories", null, 1, true, null, UtcNow);
        var childA = Category.Create("Totes", "totes", rootA.Id, 2, true, null, UtcNow);
        var childB = Category.Create("Backpacks", "backpacks", rootA.Id, 1, true, null, UtcNow);

        var repository = new Mock<ICategoryRepository>();
        repository
            .Setup(x => x.ListActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([rootA, rootB, childA, childB]);

        var sut = new GetCategoryTreeQueryHandler(repository.Object);

        var result = await sut.Handle(new GetCategoryTreeQuery(), CancellationToken.None);

        result.Select(x => x.Name).Should().Equal("Accessories", "Bags");
        result[1].Children.Select(x => x.Name).Should().Equal("Backpacks", "Totes");
    }
}
