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

        if (!await ColumnExistsAsync(dbContext, "LostFoundClaims", "PreferredContactMethod"))
        {
            await dbContext.Database.ExecuteSqlRawAsync("""ALTER TABLE "LostFoundClaims" ADD COLUMN "PreferredContactMethod" TEXT NULL;""");
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
