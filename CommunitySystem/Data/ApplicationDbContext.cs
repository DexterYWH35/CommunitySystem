using CommunitySystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<Notice> Notices => Set<Notice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Post>(entity =>
        {
            entity.Property(post => post.Title)
                .HasMaxLength(120);

            entity.Property(post => post.AuthorName)
                .HasMaxLength(80);

            entity.Property(post => post.Content)
                .HasMaxLength(4000);

            entity.Property(post => post.ImagePath)
                .HasMaxLength(260);

            entity.Property(post => post.OwnerUserId)
                .HasMaxLength(450);

            entity.HasOne(post => post.OwnerUser)
                .WithMany(user => user.Posts)
                .HasForeignKey(post => post.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(post => post.Comments)
                .WithOne(comment => comment.Post)
                .HasForeignKey(comment => comment.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(post => post.Likes)
                .WithOne(like => like.Post)
                .HasForeignKey(like => like.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.Property(comment => comment.AuthorName)
                .HasMaxLength(80);

            entity.Property(comment => comment.Body)
                .HasMaxLength(1000);

            entity.Property(comment => comment.OwnerUserId)
                .HasMaxLength(450);

            entity.HasOne(comment => comment.OwnerUser)
                .WithMany(user => user.Comments)
                .HasForeignKey(comment => comment.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(comment => comment.Likes)
                .WithOne(like => like.Comment)
                .HasForeignKey(like => like.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostLike>(entity =>
        {
            entity.HasIndex(like => new { like.PostId, like.UserId })
                .IsUnique();

            entity.Property(like => like.UserId)
                .HasMaxLength(450);

            entity.HasOne(like => like.User)
                .WithMany(user => user.PostLikes)
                .HasForeignKey(like => like.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommentLike>(entity =>
        {
            entity.HasIndex(like => new { like.CommentId, like.UserId })
                .IsUnique();

            entity.Property(like => like.UserId)
                .HasMaxLength(450);

            entity.HasOne(like => like.User)
                .WithMany(user => user.CommentLikes)
                .HasForeignKey(like => like.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notice>(entity =>
        {
            entity.Property(notice => notice.Title)
                .HasMaxLength(120);

            entity.Property(notice => notice.Content)
                .HasMaxLength(3000);

            entity.Property(notice => notice.ImagePath)
                .HasMaxLength(260);

            entity.Property(notice => notice.AttachmentPath)
                .HasMaxLength(260);

            entity.Property(notice => notice.AttachmentName)
                .HasMaxLength(180);
        });
    }
}
