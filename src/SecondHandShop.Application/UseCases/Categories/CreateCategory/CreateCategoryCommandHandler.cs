using MediatR;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Application.UseCases.Categories;
using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.UseCases.Categories.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    ICategoryHierarchyCache categoryHierarchyCache,
    IClock clock) : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Category name is required.");
        }

        if (request.Name.Trim().Length > Category.NameMaxLength)
        {
            throw new ValidationException(
                $"Category name cannot exceed {Category.NameMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new ValidationException("Category slug is required.");
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        SlugValidator.EnsureValid(normalizedSlug, nameof(request.Slug));

        if (await categoryRepository.SlugExistsAsync(normalizedSlug, cancellationToken))
        {
            throw new ConflictException($"Category slug '{normalizedSlug}' already exists.");
        }

        if (request.ParentId.HasValue)
        {
            var parent = await categoryRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                throw new KeyNotFoundException($"Parent category '{request.ParentId}' was not found.");
            }

            await EnsureParentChainIsAcyclicAsync(parent, cancellationToken);
        }

        var category = Category.Create(
            request.Name,
            normalizedSlug,
            request.ParentId,
            request.SortOrder,
            request.IsActive,
            request.CreatedByAdminUserId,
            clock.UtcNow);

        await categoryRepository.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        categoryHierarchyCache.Invalidate();

        return new CategoryDto(category.Id, category.Name, category.Slug, []);
    }

    private async Task EnsureParentChainIsAcyclicAsync(Category parent, CancellationToken cancellationToken)
    {
        var visitedCategoryIds = new HashSet<Guid>();
        Category? current = parent;

        while (current is not null)
        {
            if (!visitedCategoryIds.Add(current.Id))
            {
                throw new DomainRuleViolationException("Circular category hierarchy detected.");
            }

            if (!current.ParentId.HasValue)
            {
                break;
            }

            current = await categoryRepository.GetByIdAsync(current.ParentId.Value, cancellationToken);
        }
    }
}
