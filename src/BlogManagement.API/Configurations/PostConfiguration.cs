using BlogManagement.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogManagement.API.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(220);

        builder.HasIndex(p => p.Slug)
            .IsUnique()
            .HasDatabaseName("IX_Posts_Slug");

        builder.Property(p => p.Excerpt)
            .HasMaxLength(500);

        builder.Property(p => p.Content)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Posts_CreatedAt");

        // ARCHITECTURAL INTENT: DeleteBehavior.Restrict is locked per BR-07 so deleting a Category
        // that still owns Posts is rejected (409 Conflict) rather than cascading and deleting Posts.
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
