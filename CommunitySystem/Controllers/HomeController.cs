using System.Diagnostics;
using CommunitySystem.Data;
using Microsoft.AspNetCore.Mvc;
using CommunitySystem.Models;
using CommunitySystem.Security;
using CommunitySystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CommunitySystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _dbContext;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var notices = User.Identity?.IsAuthenticated == true
            ? await _dbContext.Notices
                .AsNoTracking()
                .Where(notice => notice.IsPublished)
                .OrderByDescending(notice => notice.IsPinned)
                .ThenByDescending(notice => notice.CreatedAtUtc)
                .Take(5)
                .ToListAsync()
            : new List<Notice>();

        var featuredNotice = notices.FirstOrDefault(notice => notice.IsFeatured)
            ?? notices.FirstOrDefault();

        var recentPosts = await _dbContext.Posts
            .AsNoTracking()
            .Include(post => post.Comments)
            .Include(post => post.Likes)
            .OrderByDescending(post => post.CreatedAtUtc)
            .Take(3)
            .ToListAsync();

        var mostLikedPosts = await _dbContext.Posts
            .AsNoTracking()
            .Include(post => post.Comments)
            .Include(post => post.Likes)
            .OrderByDescending(post => post.Likes.Count)
            .ThenByDescending(post => post.CreatedAtUtc)
            .Take(3)
            .ToListAsync();

        var viewModel = new HomeIndexViewModel
        {
            TotalPosts = await _dbContext.Posts.CountAsync(),
            TotalComments = await _dbContext.Comments.CountAsync(),
            LatestPostTitle = recentPosts.FirstOrDefault()?.Title,
            RecentPosts = recentPosts,
            MostLikedPosts = mostLikedPosts,
            Notices = notices,
            FeaturedNotice = featuredNotice
        };

        return View(viewModel);
    }

    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.User}")]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
