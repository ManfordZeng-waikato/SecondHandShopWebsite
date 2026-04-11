namespace SecondHandShop.Domain.Entities;

/// <summary>
/// Join entity for the V2 product-to-category assignment model.
/// Product.CategoryId remains the primary category for backward compatibility,
/// while this table stores the full selected category set.
/// </summary>
public class ProductCategory
{
    private ProductCategory()
    {
    }

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    public static ProductCategory Create(Guid productId, Guid categoryId)
    {
        return new ProductCategory
        {
            ProductId = productId,
            CategoryId = categoryId
        };
    }
}
