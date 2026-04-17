using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Application.UseCases.Categories.CreateCategory;
using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.UnitTests.UseCases.Categories;

public class CreateCategoryCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldThrow_WhenNameIsEmpty()
    {
        var sut = CreateSut();

        var act = () => sut.Handle(new CreateCategoryCommand("", "bags", null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Category name is required.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenSlugIsEmpty()
    {
        var sut = CreateSut();

        var act = () => sut.Handle(new CreateCategoryCommand("Bags", "", null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Category slug is required.");
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenSlugAlreadyExists()
    {
        var repository = new Mock<ICategoryRepository>();
        repository
            .Setup(x => x.SlugExistsAsync("bags", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut(categoryRepository: repository.Object);

        var act = () => sut.Handle(new CreateCategoryCommand("Bags", "bags", null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Category slug 'bags' already exists.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenParentDoesNotExist()
    {
        var repository = new Mock<ICategoryRepository>();
        repository
            .Setup(x => x.SlugExistsAsync("bags", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var sut = CreateSut(categoryRepository: repository.Object);
        var parentId = Guid.NewGuid();

        var act = () => sut.Handle(new CreateCategoryCommand("Bags", "bags", parentId), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Parent category '{parentId}' was not found.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenParentChainIsCircular()
    {
        var parentId = Guid.NewGuid();
        var grandParentId = Guid.NewGuid();
        var parent = Category.Create("Parent", "parent", grandParentId, 1, true, null, UtcNow);
        var grandParent = Category.Create("Grand", "grand", parentId, 1, true, null, UtcNow);

        var repository = new Mock<ICategoryRepository>();
        repository
            .Setup(x => x.SlugExistsAsync("bags", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        repository
            .Setup(x => x.GetByIdAsync(grandParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grandParent);

        var sut = CreateSut(categoryRepository: repository.Object);

        var act = () => sut.Handle(new CreateCategoryCommand("Bags", "bags", parentId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleViolationException>()
            .WithMessage("Circular category hierarchy detected.");
    }

    [Fact]
    public async Task Handle_ShouldCreateCategory_AndInvalidateCache()
    {
        Category? added = null;
        var repository = new Mock<ICategoryRepository>();
        repository
            .Setup(x => x.SlugExistsAsync("bags", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((category, _) => added = category)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var cache = new Mock<ICategoryHierarchyCache>();

        var sut = CreateSut(
            categoryRepository: repository.Object,
            unitOfWork: unitOfWork.Object,
            categoryHierarchyCache: cache.Object);

        var result = await sut.Handle(
            new CreateCategoryCommand(" Bags ", "Bags", null, 2, true, Guid.NewGuid()),
            CancellationToken.None);

        added.Should().NotBeNull();
        added!.Name.Should().Be("Bags");
        added.Slug.Should().Be("bags");
        result.Name.Should().Be("Bags");
        cache.Verify(x => x.Invalidate(), Times.Once);
    }

    private static CreateCategoryCommandHandler CreateSut(
        ICategoryRepository? categoryRepository = null,
        IUnitOfWork? unitOfWork = null,
        ICategoryHierarchyCache? categoryHierarchyCache = null)
    {
        return new CreateCategoryCommandHandler(
            categoryRepository ?? Mock.Of<ICategoryRepository>(),
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            categoryHierarchyCache ?? Mock.Of<ICategoryHierarchyCache>(),
            new StubClock(UtcNow));
    }

    private sealed class StubClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
