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
    public DbSet<LostFoundItem> LostFoundItems => Set<LostFoundItem>();
    public DbSet<LostFoundClaim> LostFoundClaims => Set<LostFoundClaim>();
    public DbSet<LostFoundLocationPreset> LostFoundLocationPresets => Set<LostFoundLocationPreset>();

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

        modelBuilder.Entity<LostFoundItem>(entity =>
        {
            entity.Property(item => item.Title)
                .HasMaxLength(120);

            entity.Property(item => item.Description)
                .HasMaxLength(3000);

            entity.Property(item => item.Category)
                .HasMaxLength(80);

            entity.Property(item => item.LocationDetails)
                .HasMaxLength(160);

            entity.Property(item => item.ContactName)
                .HasMaxLength(80);

            entity.Property(item => item.ContactEmail)
                .HasMaxLength(120);

            entity.Property(item => item.ContactPhone)
                .HasMaxLength(30);

            entity.Property(item => item.ImagePath)
                .HasMaxLength(260);

            entity.Property(item => item.ReporterUserId)
                .HasMaxLength(450);

            entity.HasOne(item => item.ReporterUser)
                .WithMany(user => user.LostFoundItems)
                .HasForeignKey(item => item.ReporterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(item => item.Claims)
                .WithOne(claim => claim.LostFoundItem)
                .HasForeignKey(claim => claim.LostFoundItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LostFoundClaim>(entity =>
        {
            entity.Property(claim => claim.ClaimerUserId)
                .HasMaxLength(450);

            entity.Property(claim => claim.ClaimantName)
                .HasMaxLength(80);

            entity.Property(claim => claim.ClaimantEmail)
                .HasMaxLength(120);

            entity.Property(claim => claim.ClaimantPhone)
                .HasMaxLength(30);

            entity.Property(claim => claim.VerificationDetails)
                .HasMaxLength(1500);

            entity.Property(claim => claim.PreferredContactMethod)
                .HasMaxLength(40);

            entity.HasIndex(claim => new { claim.LostFoundItemId, claim.ClaimerUserId })
                .IsUnique()
                .HasFilter("\"ClaimerUserId\" IS NOT NULL");

            entity.HasOne(claim => claim.ClaimerUser)
                .WithMany(user => user.LostFoundClaims)
                .HasForeignKey(claim => claim.ClaimerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LostFoundLocationPreset>(entity =>
        {
            entity.Property(location => location.Name)
                .HasMaxLength(160);

            entity.HasIndex(location => location.Name)
                .IsUnique();
        });
    }
}
