using FluentAssertions;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Domain.UnitTests.Entities;

public class ProductCategoryTests
{
    [Fact]
    public void Create_ShouldStorePrimaryKeyPair()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var join = ProductCategory.Create(productId, categoryId);

        join.ProductId.Should().Be(productId);
        join.CategoryId.Should().Be(categoryId);
    }
}
