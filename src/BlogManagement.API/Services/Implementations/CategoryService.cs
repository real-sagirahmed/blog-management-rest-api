using BlogManagement.API.Data;
using BlogManagement.API.DTOs.Categories;
using BlogManagement.API.Exceptions;
using BlogManagement.API.Helpers;
using BlogManagement.API.Models;
using BlogManagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.API.Services.Implementations;

public sealed class CategoryService(BlogDbContext context) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Description, c.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Description, c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return category ?? throw new NotFoundException($"Category with ID {id} was not found.");
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);
        var lowerName = name.ToLower();

        var exists = await context.Categories
            .AnyAsync(c => c.Name.ToLower() == lowerName, cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        var category = new Category
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        context.Categories.Add(category);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbExceptionClassifier.IsUniqueViolation(ex))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        return new CategoryResponse(category.Id, category.Name, category.Description, category.CreatedAt);
    }

    public async Task UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Category with ID {id} was not found.");

        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);
        var lowerName = name.ToLower();

        var duplicateExists = await context.Categories
            .AnyAsync(c => c.Id != id && c.Name.ToLower() == lowerName, cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        category.Name = name;
        category.Description = description;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbExceptionClassifier.IsUniqueViolation(ex))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Category with ID {id} was not found.");

        var hasPosts = await context.Posts
            .AnyAsync(p => p.CategoryId == id, cancellationToken);

        if (hasPosts)
        {
            throw new ConflictException($"Category with ID {id} cannot be deleted because it has associated posts.");
        }

        context.Categories.Remove(category);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbExceptionClassifier.IsConstraintConflict(ex))
        {
            throw new ConflictException($"Category with ID {id} cannot be deleted because it has associated posts.");
        }
    }

    private static string NormalizeName(string rawName)
    {
        var trimmed = rawName.Trim();
        if (trimmed.Length is < 2 or > 100)
        {
            throw new BadRequestException("Category name must be between 2 and 100 characters after trimming.");
        }

        return trimmed;
    }

    private static string? NormalizeDescription(string? rawDescription) =>
        string.IsNullOrWhiteSpace(rawDescription) ? null : rawDescription.Trim();
}
