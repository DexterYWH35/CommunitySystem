using CommunitySystem.Models;
using CommunitySystem.Models.Marketplace;
using CommunitySystem.Models.Notifications;
using CommunitySystem.Models.SupportChat;
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
    public DbSet<LostFoundCategoryPreset> LostFoundCategoryPresets => Set<LostFoundCategoryPreset>();
    public DbSet<ComplaintCase> ComplaintCases => Set<ComplaintCase>();
    public DbSet<ComplaintCaseImage> ComplaintCaseImages => Set<ComplaintCaseImage>();
    public DbSet<ComplaintLabel> ComplaintLabels => Set<ComplaintLabel>();
    public DbSet<ComplaintCaseLabel> ComplaintCaseLabels => Set<ComplaintCaseLabel>();
    public DbSet<ComplaintCaseUpdate> ComplaintCaseUpdates => Set<ComplaintCaseUpdate>();
    public DbSet<MarketplaceItem> MarketplaceItems => Set<MarketplaceItem>();
    public DbSet<MarketplaceItemImage> MarketplaceItemImages => Set<MarketplaceItemImage>();
    public DbSet<MarketplaceChatThread> MarketplaceChatThreads => Set<MarketplaceChatThread>();
    public DbSet<MarketplaceChatMessage> MarketplaceChatMessages => Set<MarketplaceChatMessage>();
    public DbSet<MarketplaceItemSave> MarketplaceItemSaves => Set<MarketplaceItemSave>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<SupportChatThread> SupportChatThreads => Set<SupportChatThread>();
    public DbSet<SupportChatMessage> SupportChatMessages => Set<SupportChatMessage>();
    public DbSet<SupportChatThreadRead> SupportChatThreadReads => Set<SupportChatThreadRead>();

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

            entity.Property(item => item.ResolvedAtUtc);

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

            entity.Property(claim => claim.ReviewedByUserId)
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

            entity.Property(claim => claim.AdminRemarks)
                .HasMaxLength(600);

            entity.HasIndex(claim => new { claim.LostFoundItemId, claim.ClaimerUserId })
                .IsUnique()
                .HasFilter("\"ClaimerUserId\" IS NOT NULL");

            entity.HasOne(claim => claim.ClaimerUser)
                .WithMany(user => user.LostFoundClaims)
                .HasForeignKey(claim => claim.ClaimerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(claim => claim.ReviewedByUser)
                .WithMany()
                .HasForeignKey(claim => claim.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LostFoundLocationPreset>(entity =>
        {
            entity.Property(location => location.Name)
                .HasMaxLength(160);

            entity.HasIndex(location => location.Name)
                .IsUnique();
        });

        modelBuilder.Entity<LostFoundCategoryPreset>(entity =>
        {
            entity.Property(category => category.Name)
                .HasMaxLength(80);

            entity.HasIndex(category => category.Name)
                .IsUnique();
        });

        modelBuilder.Entity<ComplaintCase>(entity =>
        {
            entity.Property(complaint => complaint.Title)
                .HasMaxLength(120);

            entity.Property(complaint => complaint.Description)
                .HasMaxLength(3000);

            entity.Property(complaint => complaint.LocationDetails)
                .HasMaxLength(160);

            entity.Property(complaint => complaint.ReporterUserId)
                .HasMaxLength(450);

            entity.HasIndex(complaint => complaint.ReporterUserId);

            entity.HasOne(complaint => complaint.ReporterUser)
                .WithMany(user => user.ComplaintCases)
                .HasForeignKey(complaint => complaint.ReporterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(complaint => complaint.Images)
                .WithOne(image => image.ComplaintCase)
                .HasForeignKey(image => image.ComplaintCaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(complaint => complaint.Labels)
                .WithOne(link => link.ComplaintCase)
                .HasForeignKey(link => link.ComplaintCaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(complaint => complaint.Updates)
                .WithOne(update => update.ComplaintCase)
                .HasForeignKey(update => update.ComplaintCaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComplaintCaseImage>(entity =>
        {
            entity.Property(image => image.ImagePath)
                .HasMaxLength(260);

            entity.HasIndex(image => image.ComplaintCaseId);
        });

        modelBuilder.Entity<ComplaintLabel>(entity =>
        {
            entity.Property(label => label.Name)
                .HasMaxLength(80);

            entity.HasIndex(label => label.Name)
                .IsUnique();
        });

        modelBuilder.Entity<ComplaintCaseLabel>(entity =>
        {
            entity.HasIndex(link => new { link.ComplaintCaseId, link.ComplaintLabelId })
                .IsUnique();

            entity.HasOne(link => link.ComplaintLabel)
                .WithMany(label => label.Cases)
                .HasForeignKey(link => link.ComplaintLabelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComplaintCaseUpdate>(entity =>
        {
            entity.Property(update => update.Remarks)
                .HasMaxLength(1500);

            entity.Property(update => update.UpdatedByUserId)
                .HasMaxLength(450);

            entity.HasIndex(update => update.ComplaintCaseId);

            entity.HasOne(update => update.UpdatedByUser)
                .WithMany()
                .HasForeignKey(update => update.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MarketplaceItem>(entity =>
        {
            entity.Property(item => item.Title)
                .HasMaxLength(120);

            entity.Property(item => item.Description)
                .HasMaxLength(3000);

            entity.Property(item => item.PaymentQrCodePath)
                .HasMaxLength(260);

            entity.Property(item => item.OwnerUserId)
                .HasMaxLength(450);

            entity.Property(item => item.Price)
                .HasConversion<double>()
                .HasColumnType("REAL");

            entity.HasIndex(item => item.IsActive);

            entity.HasOne(item => item.OwnerUser)
                .WithMany(user => user.MarketplaceItems)
                .HasForeignKey(item => item.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(item => item.Images)
                .WithOne(image => image.MarketplaceItem)
                .HasForeignKey(image => image.MarketplaceItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MarketplaceItemSave>(entity =>
        {
            entity.Property(save => save.UserId)
                .HasMaxLength(450);

            entity.HasIndex(save => new { save.MarketplaceItemId, save.UserId })
                .IsUnique();

            entity.HasIndex(save => save.UserId);

            entity.HasOne(save => save.MarketplaceItem)
                .WithMany()
                .HasForeignKey(save => save.MarketplaceItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(save => save.User)
                .WithMany(user => user.MarketplaceItemSaves)
                .HasForeignKey(save => save.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MarketplaceItemImage>(entity =>
        {
            entity.Property(image => image.ImagePath)
                .HasMaxLength(260);

            entity.HasIndex(image => image.MarketplaceItemId);
        });

        modelBuilder.Entity<MarketplaceChatThread>(entity =>
        {
            entity.Property(thread => thread.OwnerUserId)
                .HasMaxLength(450);

            entity.Property(thread => thread.BuyerUserId)
                .HasMaxLength(450);

            entity.HasIndex(thread => thread.MarketplaceItemId);

            entity.HasIndex(thread => new { thread.MarketplaceItemId, thread.OwnerUserId, thread.BuyerUserId })
                .IsUnique();

            entity.HasOne(thread => thread.OwnerUser)
                .WithMany()
                .HasForeignKey(thread => thread.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(thread => thread.BuyerUser)
                .WithMany()
                .HasForeignKey(thread => thread.BuyerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(thread => thread.MarketplaceItem)
                .WithMany()
                .HasForeignKey(thread => thread.MarketplaceItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(thread => thread.Messages)
                .WithOne(message => message.MarketplaceChatThread)
                .HasForeignKey(message => message.MarketplaceChatThreadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MarketplaceChatMessage>(entity =>
        {
            entity.Property(message => message.SenderUserId)
                .HasMaxLength(450);

            entity.Property(message => message.Body)
                .HasMaxLength(2000);

            entity.Property(message => message.ImagePath)
                .HasMaxLength(260);

            entity.HasIndex(message => message.MarketplaceChatThreadId);
            entity.HasIndex(message => message.CreatedAtUtc);

            entity.HasOne(message => message.SenderUser)
                .WithMany()
                .HasForeignKey(message => message.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupportChatThread>(entity =>
        {
            entity.Property(thread => thread.UserId)
                .HasMaxLength(450);

            entity.HasIndex(thread => thread.UserId)
                .IsUnique();

            entity.HasOne(thread => thread.User)
                .WithMany()
                .HasForeignKey(thread => thread.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(thread => thread.Messages)
                .WithOne(message => message.SupportChatThread)
                .HasForeignKey(message => message.SupportChatThreadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupportChatMessage>(entity =>
        {
            entity.Property(message => message.SenderUserId)
                .HasMaxLength(450);

            entity.Property(message => message.Body)
                .HasMaxLength(2000);

            entity.HasIndex(message => message.SupportChatThreadId);
            entity.HasIndex(message => message.CreatedAtUtc);

            entity.HasOne(message => message.SenderUser)
                .WithMany()
                .HasForeignKey(message => message.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupportChatThreadRead>(entity =>
        {
            entity.Property(read => read.UserId)
                .HasMaxLength(450);

            entity.HasIndex(read => new { read.SupportChatThreadId, read.UserId })
                .IsUnique();

            entity.HasIndex(read => read.UserId);

            entity.HasOne(read => read.SupportChatThread)
                .WithMany()
                .HasForeignKey(read => read.SupportChatThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(read => read.User)
                .WithMany()
                .HasForeignKey(read => read.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.Property(notification => notification.RecipientUserId)
                .HasMaxLength(450);

            entity.Property(notification => notification.ActorUserId)
                .HasMaxLength(450);

            entity.Property(notification => notification.Title)
                .HasMaxLength(180);

            entity.Property(notification => notification.Body)
                .HasMaxLength(600);

            entity.Property(notification => notification.LinkUrl)
                .HasMaxLength(260);

            entity.HasIndex(notification => new { notification.RecipientUserId, notification.IsRead });
            entity.HasIndex(notification => notification.CreatedAtUtc);

            entity.HasOne(notification => notification.RecipientUser)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(notification => notification.ActorUser)
                .WithMany()
                .HasForeignKey(notification => notification.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
