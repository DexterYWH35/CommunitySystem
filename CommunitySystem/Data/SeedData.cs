using CommunitySystem.Models;
using CommunitySystem.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleName in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var adminUser = await EnsureUserAsync(
            userManager,
            "admin@communitysystem.local",
            "Admin123",
            RoleNames.Admin);

        var standardUser = await EnsureUserAsync(
            userManager,
            "user@communitysystem.local",
            "User123",
            RoleNames.User);

        if (await dbContext.Posts.AnyAsync())
        {
            var existingPosts = await dbContext.Posts
                .Where(post => post.OwnerUserId == null)
                .OrderBy(post => post.Id)
                .ToListAsync();

            for (var index = 0; index < existingPosts.Count; index++)
            {
                existingPosts[index].OwnerUserId = index % 2 == 0 ? adminUser.Id : standardUser.Id;
                existingPosts[index].AuthorName = index % 2 == 0 ? adminUser.Email! : standardUser.Email!;
            }

            var existingComments = await dbContext.Comments
                .Where(comment => comment.OwnerUserId == null)
                .OrderBy(comment => comment.Id)
                .ToListAsync();

            for (var index = 0; index < existingComments.Count; index++)
            {
                existingComments[index].OwnerUserId = index % 2 == 0 ? standardUser.Id : adminUser.Id;
                existingComments[index].AuthorName = index % 2 == 0 ? standardUser.Email! : adminUser.Email!;
            }

            await dbContext.SaveChangesAsync();

            await SeedLikesAsync(dbContext, adminUser, standardUser);
            await SeedNoticesAsync(dbContext);
            await SeedLostFoundAsync(dbContext, adminUser, standardUser);
            await SeedLostFoundLocationsAsync(dbContext);
            return;
        }

        var posts = new List<Post>
        {
            new()
            {
                Title = "Spring volunteer intake is now open",
                AuthorName = "Program Team",
                OwnerUserId = adminUser.Id,
                Content = "We are opening volunteer sign-up for the spring season. This intake covers weekend event support, outreach coordination, and registration assistance. Please review role descriptions and confirm availability early so scheduling can begin smoothly.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-9),
                Comments =
                {
                    new Comment
                    {
                        AuthorName = "Alicia",
                        OwnerUserId = standardUser.Id,
                        Body = "Can we include orientation dates in the next update?",
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-11)
                    },
                    new Comment
                    {
                        AuthorName = "Marcus",
                        OwnerUserId = adminUser.Id,
                        Body = "I can help with registration support on Saturdays.",
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
                    }
                }
            },
            new()
            {
                Title = "Food distribution pilot feedback",
                AuthorName = "Operations Lead",
                OwnerUserId = adminUser.Id,
                Content = "The first week of the food distribution pilot showed strong uptake from returning families. We should document queue times, volunteer allocation, and stock issues before expanding the model to another neighborhood location.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-7),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-5),
                Comments =
                {
                    new Comment
                    {
                        AuthorName = "Jin",
                        OwnerUserId = standardUser.Id,
                        Body = "Queue management improved once check-in was split into two lanes.",
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-6)
                    }
                }
            },
            new()
            {
                Title = "Digital literacy workshop planning",
                AuthorName = "Community Outreach",
                OwnerUserId = standardUser.Id,
                Content = "We are drafting the next digital literacy workshop series with a focus on device basics, online safety, and practical job-search tools. Partner venue options and facilitator availability need to be finalized this week.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            }
        };

        await dbContext.Posts.AddRangeAsync(posts);
        await dbContext.SaveChangesAsync();
        await SeedLikesAsync(dbContext, adminUser, standardUser);
        await SeedNoticesAsync(dbContext);
        await SeedLostFoundAsync(dbContext, adminUser, standardUser);
        await SeedLostFoundLocationsAsync(dbContext);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to seed user '{email}': {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task SeedLikesAsync(ApplicationDbContext dbContext, ApplicationUser adminUser, ApplicationUser standardUser)
    {
        if (!await dbContext.PostLikes.AnyAsync())
        {
            var posts = await dbContext.Posts.OrderBy(post => post.Id).ToListAsync();
            if (posts.Count > 0)
            {
                dbContext.PostLikes.AddRange(
                    new PostLike { PostId = posts[0].Id, UserId = standardUser.Id, CreatedAtUtc = DateTime.UtcNow.AddDays(-2) },
                    new PostLike { PostId = posts[0].Id, UserId = adminUser.Id, CreatedAtUtc = DateTime.UtcNow.AddDays(-1) });

                if (posts.Count > 1)
                {
                    dbContext.PostLikes.Add(new PostLike
                    {
                        PostId = posts[1].Id,
                        UserId = standardUser.Id,
                        CreatedAtUtc = DateTime.UtcNow.AddHours(-18)
                    });
                }
            }
        }

        if (!await dbContext.CommentLikes.AnyAsync())
        {
            var comments = await dbContext.Comments.OrderBy(comment => comment.Id).ToListAsync();
            if (comments.Count > 0)
            {
                dbContext.CommentLikes.Add(new CommentLike
                {
                    CommentId = comments[0].Id,
                    UserId = adminUser.Id,
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-12)
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedNoticesAsync(ApplicationDbContext dbContext)
    {
        if (await dbContext.Notices.AnyAsync())
        {
            return;
        }

        dbContext.Notices.AddRange(
            new Notice
            {
                Title = "Management Office Operating Hours",
                Content = "The management office will operate from 9:00 AM to 6:00 PM on weekdays and 9:00 AM to 1:00 PM on Saturdays. Sunday services remain closed unless otherwise announced.",
                IsPublished = true,
                IsPinned = true,
                IsFeatured = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-4)
            },
            new Notice
            {
                Title = "Water Tank Maintenance Notice",
                Content = "Scheduled maintenance for the rooftop water tank will take place this Friday from 10:00 AM to 2:00 PM. Temporary pressure interruptions may occur during the maintenance window.",
                IsPublished = true,
                IsPinned = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedLostFoundAsync(ApplicationDbContext dbContext, ApplicationUser adminUser, ApplicationUser standardUser)
    {
        if (await dbContext.LostFoundItems.AnyAsync())
        {
            return;
        }

        dbContext.LostFoundItems.AddRange(
            new LostFoundItem
            {
                Title = "Black water bottle at lobby seating area",
                Description = "A reusable black bottle was found near the lobby sofas after the weekend family event. The bottle has a silver cap and a small sticker near the base.",
                Category = "Bottle",
                LocationDetails = "Tower A lobby seating area",
                ListingType = LostFoundListingType.Found,
                ContactName = "Lobby Desk Team",
                ContactEmail = adminUser.Email,
                ContactPhone = "012-3456789",
                ReporterUserId = adminUser.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                IncidentDateUtc = DateTime.UtcNow.AddDays(-3).Date
            },
            new LostFoundItem
            {
                Title = "Lost keychain with access card",
                Description = "Resident misplaced a blue fabric keychain attached to one silver key and a building access card. Last seen after returning from the gym.",
                Category = "Keys",
                LocationDetails = "Between gym level and Tower B lift lobby",
                ListingType = LostFoundListingType.Lost,
                ContactName = "Resident Owner",
                ContactEmail = standardUser.Email,
                ReporterUserId = standardUser.Id,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                IncidentDateUtc = DateTime.UtcNow.AddDays(-2).Date
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedLostFoundLocationsAsync(ApplicationDbContext dbContext)
    {
        if (await dbContext.LostFoundLocationPresets.AnyAsync())
        {
            return;
        }

        dbContext.LostFoundLocationPresets.AddRange(
            new LostFoundLocationPreset { Name = "Tower A lobby", DisplayOrder = 10 },
            new LostFoundLocationPreset { Name = "Tower B lift lobby", DisplayOrder = 20 },
            new LostFoundLocationPreset { Name = "Gym level", DisplayOrder = 30 },
            new LostFoundLocationPreset { Name = "Management office", DisplayOrder = 40 },
            new LostFoundLocationPreset { Name = "Parking level", DisplayOrder = 50 });

        await dbContext.SaveChangesAsync();
    }
}
