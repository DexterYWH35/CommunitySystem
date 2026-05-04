using System.Data;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Data;

public static class DatabaseInitializer
{
    public static async Task EnsureSchemaAsync(ApplicationDbContext dbContext)
    {
        await dbContext.Database.EnsureCreatedAsync();

        if (!await TableExistsAsync(dbContext, "AspNetUsers"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "AspNetRoles" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetRoles" PRIMARY KEY,
                    "Name" TEXT NULL,
                    "NormalizedName" TEXT NULL,
                    "ConcurrencyStamp" TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS "AspNetUsers" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
                    "UserName" TEXT NULL,
                    "NormalizedUserName" TEXT NULL,
                    "Email" TEXT NULL,
                    "NormalizedEmail" TEXT NULL,
                    "EmailConfirmed" INTEGER NOT NULL,
                    "PasswordHash" TEXT NULL,
                    "SecurityStamp" TEXT NULL,
                    "ConcurrencyStamp" TEXT NULL,
                    "PhoneNumber" TEXT NULL,
                    "PhoneNumberConfirmed" INTEGER NOT NULL,
                    "TwoFactorEnabled" INTEGER NOT NULL,
                    "LockoutEnd" TEXT NULL,
                    "LockoutEnabled" INTEGER NOT NULL,
                    "AccessFailedCount" INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY AUTOINCREMENT,
                    "RoleId" TEXT NOT NULL,
                    "ClaimType" TEXT NULL,
                    "ClaimValue" TEXT NULL,
                    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY AUTOINCREMENT,
                    "UserId" TEXT NOT NULL,
                    "ClaimType" TEXT NULL,
                    "ClaimValue" TEXT NULL,
                    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
                    "LoginProvider" TEXT NOT NULL,
                    "ProviderKey" TEXT NOT NULL,
                    "ProviderDisplayName" TEXT NULL,
                    "UserId" TEXT NOT NULL,
                    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
                    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
                    "UserId" TEXT NOT NULL,
                    "RoleId" TEXT NOT NULL,
                    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
                    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
                    "UserId" TEXT NOT NULL,
                    "LoginProvider" TEXT NOT NULL,
                    "Name" TEXT NOT NULL,
                    "Value" TEXT NULL,
                    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
                    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");
                CREATE INDEX IF NOT EXISTS "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");
                CREATE INDEX IF NOT EXISTS "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
                CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");
                CREATE INDEX IF NOT EXISTS "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");
                CREATE INDEX IF NOT EXISTS "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");
                CREATE INDEX IF NOT EXISTS "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");
                """
            );
        }

        if (!await ColumnExistsAsync(dbContext, "Posts", "OwnerUserId"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "Posts" ADD COLUMN "OwnerUserId" TEXT NULL;""");
        }

        if (!await ColumnExistsAsync(dbContext, "Posts", "ImagePath"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "Posts" ADD COLUMN "ImagePath" TEXT NULL;""");
        }

        if (!await ColumnExistsAsync(dbContext, "Comments", "OwnerUserId"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "Comments" ADD COLUMN "OwnerUserId" TEXT NULL;""");
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "PostLikes" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PostLikes" PRIMARY KEY AUTOINCREMENT,
                "PostId" INTEGER NOT NULL,
                "UserId" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_PostLikes_Posts_PostId" FOREIGN KEY ("PostId") REFERENCES "Posts" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_PostLikes_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "CommentLikes" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_CommentLikes" PRIMARY KEY AUTOINCREMENT,
                "CommentId" INTEGER NOT NULL,
                "UserId" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_CommentLikes_Comments_CommentId" FOREIGN KEY ("CommentId") REFERENCES "Comments" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_CommentLikes_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_PostLikes_PostId_UserId" ON "PostLikes" ("PostId", "UserId");
            CREATE INDEX IF NOT EXISTS "IX_PostLikes_UserId" ON "PostLikes" ("UserId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_CommentLikes_CommentId_UserId" ON "CommentLikes" ("CommentId", "UserId");
            CREATE INDEX IF NOT EXISTS "IX_CommentLikes_UserId" ON "CommentLikes" ("UserId");
            """
        );

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "Notices" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Notices" PRIMARY KEY AUTOINCREMENT,
                "Title" TEXT NOT NULL,
                "Content" TEXT NOT NULL,
                "ImagePath" TEXT NULL,
                "AttachmentPath" TEXT NULL,
                "AttachmentName" TEXT NULL,
                "IsPublished" INTEGER NOT NULL,
                "IsPinned" INTEGER NOT NULL DEFAULT 0,
                "IsFeatured" INTEGER NOT NULL DEFAULT 0,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL
            );
            """
        );

        if (!await ColumnExistsAsync(dbContext, "Notices", "AttachmentPath"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "Notices" ADD COLUMN "AttachmentPath" TEXT NULL;""");
        }

        if (!await ColumnExistsAsync(dbContext, "Notices", "AttachmentName"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "Notices" ADD COLUMN "AttachmentName" TEXT NULL;""");
        }

        if (!await ColumnExistsAsync(dbContext, "Notices", "IsPinned"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "Notices" ADD COLUMN "IsPinned" INTEGER NOT NULL DEFAULT 0;""");
        }

        if (!await ColumnExistsAsync(dbContext, "Notices", "IsFeatured"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "Notices" ADD COLUMN "IsFeatured" INTEGER NOT NULL DEFAULT 0;""");
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "LostFoundItems" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_LostFoundItems" PRIMARY KEY AUTOINCREMENT,
                "Title" TEXT NOT NULL,
                "Description" TEXT NOT NULL,
                "Category" TEXT NOT NULL,
                "LocationDetails" TEXT NOT NULL,
                "ListingType" INTEGER NOT NULL,
                "Status" INTEGER NOT NULL DEFAULT 1,
                "IncidentDateUtc" TEXT NULL,
                "ContactName" TEXT NOT NULL,
                "ContactEmail" TEXT NULL,
                "ContactPhone" TEXT NULL,
                "ImagePath" TEXT NULL,
                "ReporterUserId" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL,
                CONSTRAINT "FK_LostFoundItems_AspNetUsers_ReporterUserId" FOREIGN KEY ("ReporterUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
            );

            CREATE TABLE IF NOT EXISTS "LostFoundClaims" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_LostFoundClaims" PRIMARY KEY AUTOINCREMENT,
                "LostFoundItemId" INTEGER NOT NULL,
                "ClaimerUserId" TEXT NULL,
                "ClaimantName" TEXT NOT NULL,
                "ClaimantEmail" TEXT NOT NULL,
                "ClaimantPhone" TEXT NOT NULL,
                "VerificationDetails" TEXT NOT NULL,
                "PreferredContactMethod" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_LostFoundClaims_LostFoundItems_LostFoundItemId" FOREIGN KEY ("LostFoundItemId") REFERENCES "LostFoundItems" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_LostFoundClaims_AspNetUsers_ClaimerUserId" FOREIGN KEY ("ClaimerUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS "IX_LostFoundItems_ReporterUserId" ON "LostFoundItems" ("ReporterUserId");
            CREATE INDEX IF NOT EXISTS "IX_LostFoundClaims_LostFoundItemId" ON "LostFoundClaims" ("LostFoundItemId");
            CREATE INDEX IF NOT EXISTS "IX_LostFoundClaims_ClaimerUserId" ON "LostFoundClaims" ("ClaimerUserId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_LostFoundClaims_LostFoundItemId_ClaimerUserId"
                ON "LostFoundClaims" ("LostFoundItemId", "ClaimerUserId")
                WHERE "ClaimerUserId" IS NOT NULL;
            """
        );

        if (!await ColumnExistsAsync(dbContext, "LostFoundItems", "Status"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundItems" ADD COLUMN "Status" INTEGER NOT NULL DEFAULT 1;""");
        }

        if (!await ColumnExistsAsync(dbContext, "LostFoundItems", "IncidentDateUtc"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundItems" ADD COLUMN "IncidentDateUtc" TEXT NULL;""");
        }

        if (!await ColumnExistsAsync(dbContext, "LostFoundItems", "ContactPhone"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundItems" ADD COLUMN "ContactPhone" TEXT NULL;""");
        }

        if (!await ColumnExistsAsync(dbContext, "LostFoundItems", "ResolvedAtUtc"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundItems" ADD COLUMN "ResolvedAtUtc" TEXT NULL;""");
        }

        if (!await ColumnExistsAsync(dbContext, "LostFoundClaims", "PreferredContactMethod"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundClaims" ADD COLUMN "PreferredContactMethod" TEXT NULL;""");
        }

        if (!await ColumnExistsAsync(dbContext, "LostFoundClaims", "Status"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundClaims" ADD COLUMN "Status" INTEGER NOT NULL DEFAULT 1;""");
        }

        if (!await ColumnExistsAsync(dbContext, "LostFoundClaims", "ReviewedAtUtc"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundClaims" ADD COLUMN "ReviewedAtUtc" TEXT NULL;""");
        }

        if (!await ColumnExistsAsync(dbContext, "LostFoundClaims", "ReviewedByUserId"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundClaims" ADD COLUMN "ReviewedByUserId" TEXT NULL;""");
        }

        if (!await ColumnExistsAsync(dbContext, "LostFoundClaims", "AdminRemarks"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundClaims" ADD COLUMN "AdminRemarks" TEXT NULL;""");
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "LostFoundLocationPresets" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_LostFoundLocationPresets" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "IsActive" INTEGER NOT NULL DEFAULT 1,
                "DisplayOrder" INTEGER NOT NULL DEFAULT 0,
                "CreatedAtUtc" TEXT NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_LostFoundLocationPresets_Name" ON "LostFoundLocationPresets" ("Name");
            """
        );

        if (!await ColumnExistsAsync(dbContext, "LostFoundLocationPresets", "IsActive"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundLocationPresets" ADD COLUMN "IsActive" INTEGER NOT NULL DEFAULT 1;""");
        }

        if (!await ColumnExistsAsync(dbContext, "LostFoundLocationPresets", "DisplayOrder"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundLocationPresets" ADD COLUMN "DisplayOrder" INTEGER NOT NULL DEFAULT 0;""");
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "LostFoundCategoryPresets" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_LostFoundCategoryPresets" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "IsActive" INTEGER NOT NULL DEFAULT 1,
                "DisplayOrder" INTEGER NOT NULL DEFAULT 0,
                "CreatedAtUtc" TEXT NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_LostFoundCategoryPresets_Name" ON "LostFoundCategoryPresets" ("Name");
            """
        );

        if (!await ColumnExistsAsync(dbContext, "LostFoundCategoryPresets", "IsActive"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundCategoryPresets" ADD COLUMN "IsActive" INTEGER NOT NULL DEFAULT 1;""");
        }

        if (!await ColumnExistsAsync(dbContext, "LostFoundCategoryPresets", "DisplayOrder"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundCategoryPresets" ADD COLUMN "DisplayOrder" INTEGER NOT NULL DEFAULT 0;""");
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "ComplaintCases" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ComplaintCases" PRIMARY KEY AUTOINCREMENT,
                "Title" TEXT NOT NULL,
                "Description" TEXT NOT NULL,
                "LocationDetails" TEXT NOT NULL,
                "Status" INTEGER NOT NULL DEFAULT 1,
                "ReporterUserId" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL,
                CONSTRAINT "FK_ComplaintCases_AspNetUsers_ReporterUserId" FOREIGN KEY ("ReporterUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
            );

            CREATE TABLE IF NOT EXISTS "ComplaintCaseImages" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ComplaintCaseImages" PRIMARY KEY AUTOINCREMENT,
                "ComplaintCaseId" INTEGER NOT NULL,
                "ImagePath" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_ComplaintCaseImages_ComplaintCases_ComplaintCaseId" FOREIGN KEY ("ComplaintCaseId") REFERENCES "ComplaintCases" ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_ComplaintCases_ReporterUserId" ON "ComplaintCases" ("ReporterUserId");
            CREATE INDEX IF NOT EXISTS "IX_ComplaintCaseImages_ComplaintCaseId" ON "ComplaintCaseImages" ("ComplaintCaseId");
            """
        );

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "ComplaintLabels" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ComplaintLabels" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ComplaintLabels_Name" ON "ComplaintLabels" ("Name");

            CREATE TABLE IF NOT EXISTS "ComplaintCaseLabels" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ComplaintCaseLabels" PRIMARY KEY AUTOINCREMENT,
                "ComplaintCaseId" INTEGER NOT NULL,
                "ComplaintLabelId" INTEGER NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_ComplaintCaseLabels_ComplaintCases_ComplaintCaseId" FOREIGN KEY ("ComplaintCaseId") REFERENCES "ComplaintCases" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_ComplaintCaseLabels_ComplaintLabels_ComplaintLabelId" FOREIGN KEY ("ComplaintLabelId") REFERENCES "ComplaintLabels" ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ComplaintCaseLabels_ComplaintCaseId_ComplaintLabelId"
                ON "ComplaintCaseLabels" ("ComplaintCaseId", "ComplaintLabelId");

            CREATE INDEX IF NOT EXISTS "IX_ComplaintCaseLabels_ComplaintLabelId" ON "ComplaintCaseLabels" ("ComplaintLabelId");

            CREATE TABLE IF NOT EXISTS "ComplaintCaseUpdates" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ComplaintCaseUpdates" PRIMARY KEY AUTOINCREMENT,
                "ComplaintCaseId" INTEGER NOT NULL,
                "Status" INTEGER NOT NULL,
                "Remarks" TEXT NULL,
                "UpdatedByUserId" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_ComplaintCaseUpdates_ComplaintCases_ComplaintCaseId" FOREIGN KEY ("ComplaintCaseId") REFERENCES "ComplaintCases" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_ComplaintCaseUpdates_AspNetUsers_UpdatedByUserId" FOREIGN KEY ("UpdatedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS "IX_ComplaintCaseUpdates_ComplaintCaseId" ON "ComplaintCaseUpdates" ("ComplaintCaseId");
            CREATE INDEX IF NOT EXISTS "IX_ComplaintCaseUpdates_UpdatedByUserId" ON "ComplaintCaseUpdates" ("UpdatedByUserId");
            """
        );

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "MarketplaceItems" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MarketplaceItems" PRIMARY KEY AUTOINCREMENT,
                "Title" TEXT NOT NULL,
                "Description" TEXT NOT NULL,
                "ListingType" INTEGER NOT NULL,
                "Price" REAL NOT NULL,
                "PaymentQrCodePath" TEXT NULL,
                "IsActive" INTEGER NOT NULL DEFAULT 1,
                "OwnerUserId" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL,
                CONSTRAINT "FK_MarketplaceItems_AspNetUsers_OwnerUserId" FOREIGN KEY ("OwnerUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
            );

            CREATE TABLE IF NOT EXISTS "MarketplaceItemImages" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MarketplaceItemImages" PRIMARY KEY AUTOINCREMENT,
                "MarketplaceItemId" INTEGER NOT NULL,
                "ImagePath" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_MarketplaceItemImages_MarketplaceItems_MarketplaceItemId" FOREIGN KEY ("MarketplaceItemId") REFERENCES "MarketplaceItems" ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_MarketplaceItems_OwnerUserId" ON "MarketplaceItems" ("OwnerUserId");
            CREATE INDEX IF NOT EXISTS "IX_MarketplaceItems_IsActive" ON "MarketplaceItems" ("IsActive");
            CREATE INDEX IF NOT EXISTS "IX_MarketplaceItemImages_MarketplaceItemId" ON "MarketplaceItemImages" ("MarketplaceItemId");

            CREATE TABLE IF NOT EXISTS "MarketplaceItemSaves" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MarketplaceItemSaves" PRIMARY KEY AUTOINCREMENT,
                "MarketplaceItemId" INTEGER NOT NULL,
                "UserId" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_MarketplaceItemSaves_MarketplaceItems_MarketplaceItemId" FOREIGN KEY ("MarketplaceItemId") REFERENCES "MarketplaceItems" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_MarketplaceItemSaves_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_MarketplaceItemSaves_Item_User"
                ON "MarketplaceItemSaves" ("MarketplaceItemId", "UserId");
            CREATE INDEX IF NOT EXISTS "IX_MarketplaceItemSaves_UserId" ON "MarketplaceItemSaves" ("UserId");

            CREATE TABLE IF NOT EXISTS "MarketplaceChatThreads" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MarketplaceChatThreads" PRIMARY KEY AUTOINCREMENT,
                "MarketplaceItemId" INTEGER NOT NULL,
                "OwnerUserId" TEXT NOT NULL,
                "BuyerUserId" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "LastMessageAtUtc" TEXT NULL,
                CONSTRAINT "FK_MarketplaceChatThreads_MarketplaceItems_MarketplaceItemId" FOREIGN KEY ("MarketplaceItemId") REFERENCES "MarketplaceItems" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_MarketplaceChatThreads_AspNetUsers_OwnerUserId" FOREIGN KEY ("OwnerUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_MarketplaceChatThreads_AspNetUsers_BuyerUserId" FOREIGN KEY ("BuyerUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_MarketplaceChatThreads_Item_Owner_Buyer"
                ON "MarketplaceChatThreads" ("MarketplaceItemId", "OwnerUserId", "BuyerUserId");
            CREATE INDEX IF NOT EXISTS "IX_MarketplaceChatThreads_MarketplaceItemId" ON "MarketplaceChatThreads" ("MarketplaceItemId");
            CREATE INDEX IF NOT EXISTS "IX_MarketplaceChatThreads_OwnerUserId" ON "MarketplaceChatThreads" ("OwnerUserId");
            CREATE INDEX IF NOT EXISTS "IX_MarketplaceChatThreads_BuyerUserId" ON "MarketplaceChatThreads" ("BuyerUserId");

            CREATE TABLE IF NOT EXISTS "MarketplaceChatMessages" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MarketplaceChatMessages" PRIMARY KEY AUTOINCREMENT,
                "MarketplaceChatThreadId" INTEGER NOT NULL,
                "SenderUserId" TEXT NOT NULL,
                "Body" TEXT NOT NULL,
                "ImagePath" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_MarketplaceChatMessages_MarketplaceChatThreads_MarketplaceChatThreadId" FOREIGN KEY ("MarketplaceChatThreadId") REFERENCES "MarketplaceChatThreads" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_MarketplaceChatMessages_AspNetUsers_SenderUserId" FOREIGN KEY ("SenderUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS "IX_MarketplaceChatMessages_MarketplaceChatThreadId" ON "MarketplaceChatMessages" ("MarketplaceChatThreadId");
            CREATE INDEX IF NOT EXISTS "IX_MarketplaceChatMessages_CreatedAtUtc" ON "MarketplaceChatMessages" ("CreatedAtUtc");
            """
        );

        if (!await ColumnExistsAsync(dbContext, "MarketplaceChatMessages", "ImagePath"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "MarketplaceChatMessages" ADD COLUMN "ImagePath" TEXT NULL;""");
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "SupportChatThreads" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SupportChatThreads" PRIMARY KEY AUTOINCREMENT,
                "UserId" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "LastMessageAtUtc" TEXT NULL,
                CONSTRAINT "FK_SupportChatThreads_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_SupportChatThreads_UserId" ON "SupportChatThreads" ("UserId");
            """
        );

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "SupportChatMessages" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SupportChatMessages" PRIMARY KEY AUTOINCREMENT,
                "SupportChatThreadId" INTEGER NOT NULL,
                "SenderUserId" TEXT NOT NULL,
                "Body" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_SupportChatMessages_SupportChatThreads_SupportChatThreadId" FOREIGN KEY ("SupportChatThreadId") REFERENCES "SupportChatThreads" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_SupportChatMessages_AspNetUsers_SenderUserId" FOREIGN KEY ("SenderUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS "IX_SupportChatMessages_SupportChatThreadId" ON "SupportChatMessages" ("SupportChatThreadId");
            CREATE INDEX IF NOT EXISTS "IX_SupportChatMessages_CreatedAtUtc" ON "SupportChatMessages" ("CreatedAtUtc");
            """
        );

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "SupportChatThreadReads" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SupportChatThreadReads" PRIMARY KEY AUTOINCREMENT,
                "SupportChatThreadId" INTEGER NOT NULL,
                "UserId" TEXT NOT NULL,
                "LastReadAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_SupportChatThreadReads_SupportChatThreads_SupportChatThreadId" FOREIGN KEY ("SupportChatThreadId") REFERENCES "SupportChatThreads" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_SupportChatThreadReads_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_SupportChatThreadReads_Thread_User"
                ON "SupportChatThreadReads" ("SupportChatThreadId", "UserId");
            CREATE INDEX IF NOT EXISTS "IX_SupportChatThreadReads_UserId" ON "SupportChatThreadReads" ("UserId");
            """
        );

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "UserNotifications" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_UserNotifications" PRIMARY KEY AUTOINCREMENT,
                "RecipientUserId" TEXT NOT NULL,
                "ActorUserId" TEXT NULL,
                "Type" INTEGER NOT NULL,
                "Title" TEXT NOT NULL,
                "Body" TEXT NULL,
                "LinkUrl" TEXT NULL,
                "IsRead" INTEGER NOT NULL DEFAULT 0,
                "CreatedAtUtc" TEXT NOT NULL,
                "ReadAtUtc" TEXT NULL,
                CONSTRAINT "FK_UserNotifications_AspNetUsers_RecipientUserId" FOREIGN KEY ("RecipientUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_UserNotifications_AspNetUsers_ActorUserId" FOREIGN KEY ("ActorUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS "IX_UserNotifications_Recipient_IsRead" ON "UserNotifications" ("RecipientUserId", "IsRead");
            CREATE INDEX IF NOT EXISTS "IX_UserNotifications_CreatedAtUtc" ON "UserNotifications" ("CreatedAtUtc");
            """
        );
    }

    private static async Task<bool> TableExistsAsync(ApplicationDbContext dbContext, string tableName)
    {
        await using var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(ApplicationDbContext dbContext, string tableName, string columnName)
    {
        await using var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
