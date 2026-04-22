using CommunitySystem.Data;
using CommunitySystem.Models;
using CommunitySystem.Security;
using CommunitySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.User}")]
public class CommentsController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var comments = await dbContext.Comments
            .AsNoTracking()
            .Include(comment => comment.Post)
            .Include(comment => comment.Likes)
            .OrderByDescending(comment => comment.CreatedAtUtc)
            .ToListAsync();

        return View(comments);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var comment = await dbContext.Comments
            .AsNoTracking()
            .Include(item => item.Post)
            .Include(item => item.Likes)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (comment is null)
        {
            return NotFound();
        }

        return View(comment);
    }

    public async Task<IActionResult> Create(int? postId)
    {
        var posts = await dbContext.Posts
            .AsNoTracking()
            .OrderBy(post => post.Title)
            .ToListAsync();

        if (posts.Count == 0)
        {
            TempData["StatusMessage"] = "Create a post before adding comments.";
            return RedirectToAction("Create", "Posts");
        }

        await PopulatePostsAsync(postId);

        var selectedPost = posts.FirstOrDefault(post => post.Id == postId);
        return View(new CommentFormViewModel
        {
            PostId = postId ?? posts[0].Id,
            PostTitle = selectedPost?.Title
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommentFormViewModel viewModel)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        if (!await PostExistsAsync(viewModel.PostId))
        {
            ModelState.AddModelError(nameof(viewModel.PostId), "Select a valid post.");
        }

        if (!ModelState.IsValid)
        {
            await PopulatePostsAsync(viewModel.PostId);
            return View(viewModel);
        }

        var comment = new Comment
        {
            PostId = viewModel.PostId,
            AuthorName = currentUser.Email ?? currentUser.UserName ?? "Community User",
            Body = viewModel.Body,
            CreatedAtUtc = DateTime.UtcNow,
            OwnerUserId = currentUser.Id
        };

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync();

        return RedirectToAction("Details", "Posts", new { id = viewModel.PostId });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var comment = await dbContext.Comments
            .AsNoTracking()
            .Include(item => item.Post)
            .Include(item => item.Likes)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (comment is null)
        {
            return NotFound();
        }

        if (!await CanManageCommentAsync(comment))
        {
            return Forbid();
        }

        await PopulatePostsAsync(comment.PostId);

        return View(new CommentFormViewModel
        {
            Id = comment.Id,
            PostId = comment.PostId,
            Body = comment.Body,
            PostTitle = comment.Post?.Title
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CommentFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!await PostExistsAsync(viewModel.PostId))
        {
            ModelState.AddModelError(nameof(viewModel.PostId), "Select a valid post.");
        }

        if (!ModelState.IsValid)
        {
            await PopulatePostsAsync(viewModel.PostId);
            return View(viewModel);
        }

        var comment = await dbContext.Comments.FindAsync(id);
        if (comment is null)
        {
            return NotFound();
        }

        if (!await CanManageCommentAsync(comment))
        {
            return Forbid();
        }

        comment.PostId = viewModel.PostId;
        comment.Body = viewModel.Body;

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var comment = await dbContext.Comments
            .AsNoTracking()
            .Include(item => item.Post)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (comment is null)
        {
            return NotFound();
        }

        if (!await CanManageCommentAsync(comment))
        {
            return Forbid();
        }

        return View(comment);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var comment = await dbContext.Comments.FindAsync(id);
        if (comment is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (!await CanManageCommentAsync(comment))
        {
            return Forbid();
        }

        var postId = comment.PostId;
        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync();

        return RedirectToAction("Details", "Posts", new { id = postId });
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

        var comment = await dbContext.Comments.FindAsync(id);
        if (comment is null)
        {
            return NotFound();
        }

        var existingLike = await dbContext.CommentLikes
            .FirstOrDefaultAsync(like => like.CommentId == id && like.UserId == currentUser.Id);

        if (existingLike is null)
        {
            dbContext.CommentLikes.Add(new CommentLike
            {
                CommentId = id,
                UserId = currentUser.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            dbContext.CommentLikes.Remove(existingLike);
        }

        await dbContext.SaveChangesAsync();

        if (IsAjaxRequest(Request))
        {
            var likeCount = await dbContext.CommentLikes.CountAsync(like => like.CommentId == id);
            var hasLiked = await dbContext.CommentLikes.AnyAsync(like => like.CommentId == id && like.UserId == currentUser.Id);
            return Json(new { commentId = id, likeCount, hasLiked });
        }

        return RedirectToAction("Details", "Posts", new { id = comment.PostId });
    }

    private static bool IsAjaxRequest(HttpRequest request)
    {
        return string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> PostExistsAsync(int postId)
    {
        return await dbContext.Posts.AnyAsync(post => post.Id == postId);
    }

    private async Task<bool> CanManageCommentAsync(Comment comment)
    {
        if (User.IsInRole(RoleNames.Admin))
        {
            return true;
        }

        var currentUser = await userManager.GetUserAsync(User);
        return currentUser is not null && comment.OwnerUserId == currentUser.Id;
    }

    private async Task PopulatePostsAsync(int? selectedPostId = null)
    {
        var posts = await dbContext.Posts
            .AsNoTracking()
            .OrderBy(post => post.Title)
            .ToListAsync();

        ViewBag.PostId = new SelectList(posts, nameof(Post.Id), nameof(Post.Title), selectedPostId);
    }
}
