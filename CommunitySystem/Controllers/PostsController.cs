using CommunitySystem.Data;
using CommunitySystem.Models;
using CommunitySystem.Security;
using CommunitySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.User}")]
public class PostsController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment webHostEnvironment) : Controller
{
    public async Task<IActionResult> Index()
    {
        var posts = await dbContext.Posts
            .AsNoTracking()
            .Include(post => post.Comments)
            .Include(post => post.Likes)
            .OrderByDescending(post => post.CreatedAtUtc)
            .ToListAsync();

        return View(posts);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var viewModel = await BuildPostDetailsViewModelAsync(id.Value);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    public IActionResult Create()
    {
        return View(new PostFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PostFormViewModel viewModel)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var post = new Post
        {
            Title = viewModel.Title,
            AuthorName = currentUser.Email ?? currentUser.UserName ?? "Community User",
            Content = viewModel.Content,
            CreatedAtUtc = DateTime.UtcNow,
            OwnerUserId = currentUser.Id,
            ImagePath = await SaveImageAsync(viewModel.ImageFile)
        };

        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var post = await dbContext.Posts.FindAsync(id.Value);
        if (post is null)
        {
            return NotFound();
        }

        if (!await CanManagePostAsync(post))
        {
            return Forbid();
        }

        return View(new PostFormViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            ExistingImagePath = post.ImagePath
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PostFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var post = await dbContext.Posts.FindAsync(id);
        if (post is null)
        {
            return NotFound();
        }

        if (!await CanManagePostAsync(post))
        {
            return Forbid();
        }

        post.Title = viewModel.Title;
        post.Content = viewModel.Content;
        post.UpdatedAtUtc = DateTime.UtcNow;
        if (viewModel.ImageFile is not null)
        {
            DeleteImage(post.ImagePath);
            post.ImagePath = await SaveImageAsync(viewModel.ImageFile);
        }

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var post = await dbContext.Posts
            .AsNoTracking()
            .Include(item => item.Comments)
            .Include(item => item.Likes)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (post is null)
        {
            return NotFound();
        }

        if (!await CanManagePostAsync(post))
        {
            return Forbid();
        }

        return View(post);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var post = await dbContext.Posts.FindAsync(id);
        if (post is not null)
        {
            if (!await CanManagePostAsync(post))
            {
                return Forbid();
            }

            DeleteImage(post.ImagePath);
            dbContext.Posts.Remove(post);
            await dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int id, [Bind(Prefix = "NewComment")] CommentFormViewModel newComment)
    {
        if (id != newComment.PostId)
        {
            return NotFound();
        }

        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var post = await dbContext.Posts.FindAsync(id);
        if (post is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildPostDetailsViewModelAsync(id, newComment);
            return invalidViewModel is null ? NotFound() : View("Details", invalidViewModel);
        }

        var comment = new Comment
        {
            PostId = id,
            AuthorName = currentUser.Email ?? currentUser.UserName ?? "Community User",
            Body = newComment.Body,
            CreatedAtUtc = DateTime.UtcNow,
            OwnerUserId = currentUser.Id
        };

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var post = await dbContext.Posts.FindAsync(id);
        if (post is null)
        {
            return NotFound();
        }

        var existingLike = await dbContext.PostLikes
            .FirstOrDefaultAsync(like => like.PostId == id && like.UserId == currentUser.Id);

        if (existingLike is null)
        {
            dbContext.PostLikes.Add(new PostLike
            {
                PostId = id,
                UserId = currentUser.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            dbContext.PostLikes.Remove(existingLike);
        }

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<bool> CanManagePostAsync(Post post)
    {
        if (User.IsInRole(RoleNames.Admin))
        {
            return true;
        }

        var currentUser = await userManager.GetUserAsync(User);
        return currentUser is not null && post.OwnerUserId == currentUser.Id;
    }

    private async Task<PostDetailsViewModel?> BuildPostDetailsViewModelAsync(int id, CommentFormViewModel? newComment = null)
    {
        var post = await dbContext.Posts
            .AsNoTracking()
            .Include(item => item.Comments.OrderByDescending(comment => comment.CreatedAtUtc))
            .ThenInclude(comment => comment.Likes)
            .Include(item => item.Likes)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (post is null)
        {
            return null;
        }

        return new PostDetailsViewModel
        {
            Post = post,
            Comments = post.Comments.OrderByDescending(comment => comment.CreatedAtUtc).ToList(),
            NewComment = newComment ?? new CommentFormViewModel { PostId = post.Id, PostTitle = post.Title },
            PostLikeCount = post.Likes.Count,
            HasLikedPost = User.Identity?.IsAuthenticated == true &&
                           post.Likes.Any(like => like.UserId == userManager.GetUserId(User))
        };
    }

    private async Task<string?> SaveImageAsync(IFormFile? imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return null;
        }

        var uploadsRoot = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "posts");
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(imageFile.FileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await imageFile.CopyToAsync(stream);

        return $"/uploads/posts/{fileName}";
    }

    private void DeleteImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return;
        }

        var trimmedPath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(webHostEnvironment.WebRootPath, trimmedPath);
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }
}
