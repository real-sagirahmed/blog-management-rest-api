using BlogManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.API.Data;

// ARCHITECTURAL INTENT: BlogDbContext is used directly by the Service Layer per Section 4;
// do NOT introduce a Generic Repository or redundant UnitOfWork wrapper in V1.0.
public class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BlogDbContext).Assembly);
    }
}
