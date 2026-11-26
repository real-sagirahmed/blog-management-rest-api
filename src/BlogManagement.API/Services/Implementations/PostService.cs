using BlogManagement.API.Data;
using BlogManagement.API.DTOs.Posts;
using BlogManagement.API.Exceptions;
using BlogManagement.API.Helpers;
using BlogManagement.API.Models;
using BlogManagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.API.Services.Implementations;

public sealed class PostService(BlogDbContext context) : IPostService
{
    public async Task<PostResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await context.Posts
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PostResponse(
                p.Id,
                p.Title,
                p.Slug,
                p.Excerpt,
                p.Content,
                p.CategoryId,
                p.Category.Name,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return post ?? throw new NotFoundException($"Post with ID {id} was not found.");
    }

    public async Task<PostResponse> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken = default)
    {
        var categoryName = await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        var title = NormalizeRequiredText(request.Title, minLength: 3, maxLength: 200, nameof(request.Title));
        var excerpt = NormalizeOptionalText(request.Excerpt, maxLength: 500, nameof(request.Excerpt));
        var content = NormalizeRequiredText(request.Content, minLength: 10, maxLength: null, nameof(request.Content));

        var baseSlug = SlugGenerator.GenerateBaseSlug(title);
        var (slug, nextSuffix) = await ResolveUniqueSlugAsync(baseSlug, cancellationToken);

        var post = new Post
        {
            Title = title,
            Slug = slug,
            Excerpt = excerpt,
            Content = content,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        context.Posts.Add(post);

        const int maxSaveAttempts = 5;
        for (var attempt = 1; attempt <= maxSaveAttempts; attempt++)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return new PostResponse(
                    post.Id,
                    post.Title,
                    post.Slug,
                    post.Excerpt,
                    post.Content,
                    post.CategoryId,
                    categoryName,
                    post.CreatedAt,
                    post.UpdatedAt);
            }
            catch (DbUpdateException ex) when (DbExceptionClassifier.IsUniqueViolation(ex) && attempt < maxSaveAttempts)
            {
                post.Slug = SlugGenerator.WithNumericSuffix(baseSlug, nextSuffix++);
            }
            catch (DbUpdateException ex) when (DbExceptionClassifier.IsUniqueViolation(ex))
            {
                throw new ConflictException("Could not create post because a unique slug conflict persisted. Please retry.");
            }
        }

        throw new ConflictException("Could not create post due to a unique slug conflict.");
    }

    public async Task UpdateAsync(int id, UpdatePostRequest request, CancellationToken cancellationToken = default)
    {
        var post = await context.Posts
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Post with ID {id} was not found.");

        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        // ARCHITECTURAL INTENT: Post.Slug is immutable across PUT/PATCH updates so published URLs never break.
        post.Title = NormalizeRequiredText(request.Title, minLength: 3, maxLength: 200, nameof(request.Title));
        post.Excerpt = NormalizeOptionalText(request.Excerpt, maxLength: 500, nameof(request.Excerpt));
        post.Content = NormalizeRequiredText(request.Content, minLength: 10, maxLength: null, nameof(request.Content));
        post.CategoryId = request.CategoryId;
        post.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task PatchAsync(int id, PatchPostRequest request, CancellationToken cancellationToken = default)
    {
        var post = await context.Posts
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Post with ID {id} was not found.");

        var modified = false;

        if (request.CategoryId.HasValue)
        {
            var categoryId = request.CategoryId.Value
                ?? throw new BadRequestException("CategoryId cannot be null.");
            await EnsureCategoryExistsAsync(categoryId, cancellationToken);
            post.CategoryId = categoryId;
            modified = true;
        }

        if (request.Title.HasValue)
        {
            post.Title = NormalizeRequiredText(request.Title.Value ?? string.Empty, minLength: 3, maxLength: 200, nameof(request.Title));
            modified = true;
        }

        if (request.Excerpt.HasValue)
        {
            post.Excerpt = NormalizeOptionalText(request.Excerpt.Value, maxLength: 500, nameof(request.Excerpt));
            modified = true;
        }

        if (request.Content.HasValue)
        {
            post.Content = NormalizeRequiredText(request.Content.Value ?? string.Empty, minLength: 10, maxLength: null, nameof(request.Content));
            modified = true;
        }

        if (modified)
        {
            post.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await context.Posts
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Post with ID {id} was not found.");

        context.Posts.Remove(post);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        var categoryName = await context.Categories
            .AsNoTracking()
            .Where(c => c.Id == categoryId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return categoryName ?? throw new BadRequestException($"Category with ID {categoryId} does not exist.");
    }

    private async Task<(string Slug, int NextSuffix)> ResolveUniqueSlugAsync(string baseSlug, CancellationToken cancellationToken)
    {
        var lookupPrefix = baseSlug.Length > 205 ? baseSlug[..205] : baseSlug;

        var existingSlugs = (await context.Posts
            .AsNoTracking()
            .Where(p => p.Slug == baseSlug || p.Slug.StartsWith(lookupPrefix))
            .Select(p => p.Slug)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingSlugs.Contains(baseSlug))
        {
            return (baseSlug, 2);
        }

        var suffix = 2;
        while (true)
        {
            var candidate = SlugGenerator.WithNumericSuffix(baseSlug, suffix);
            suffix++;
            if (!existingSlugs.Contains(candidate))
            {
                return (candidate, suffix);
            }
        }
    }

    private static string NormalizeRequiredText(string raw, int minLength, int? maxLength, string fieldName)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length < minLength || (maxLength.HasValue && trimmed.Length > maxLength.Value))
        {
            var rangeText = maxLength.HasValue
                ? $"between {minLength} and {maxLength.Value} characters"
                : $"at least {minLength} characters";
            throw new BadRequestException($"{fieldName} must be {rangeText} after trimming.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalText(string? raw, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new BadRequestException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return trimmed;
    }
}
