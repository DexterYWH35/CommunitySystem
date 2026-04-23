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
        var isAdmin = User.IsInRole(RoleNames.Admin);

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

        var lostFoundOpenCount = 0;
        var lostFoundUnderReviewCount = 0;
        var lostFoundResolvedCount = 0;
        var lostFoundClaimsLast7Days = 0;
        var recentLostFoundItems = new List<LostFoundItem>();
        var complaintOpenCount = 0;
        var complaintCompletedCount = 0;
        var recentComplaints = new List<ComplaintCase>();
        var noticeCount = 0;

        if (User.Identity?.IsAuthenticated == true && isAdmin)
        {
            noticeCount = await _dbContext.Notices.CountAsync();

            lostFoundOpenCount = await _dbContext.LostFoundItems.CountAsync(item => item.Status == LostFoundItemStatus.Open);
            lostFoundUnderReviewCount = await _dbContext.LostFoundItems.CountAsync(item => item.Status == LostFoundItemStatus.ClaimUnderReview);
            lostFoundResolvedCount = await _dbContext.LostFoundItems.CountAsync(item => item.Status == LostFoundItemStatus.Resolved);

            var sinceUtc = DateTime.UtcNow.AddDays(-7);
            lostFoundClaimsLast7Days = await _dbContext.LostFoundClaims.CountAsync(claim => claim.CreatedAtUtc >= sinceUtc);

            recentLostFoundItems = await _dbContext.LostFoundItems
                .AsNoTracking()
                .OrderByDescending(item => item.CreatedAtUtc)
                .Take(5)
                .ToListAsync();

            complaintOpenCount = await _dbContext.ComplaintCases.CountAsync(item =>
                item.Status == ComplaintCaseStatus.Reviewing || item.Status == ComplaintCaseStatus.Processing);
            complaintCompletedCount = await _dbContext.ComplaintCases.CountAsync(item => item.Status == ComplaintCaseStatus.Completed);

            recentComplaints = await _dbContext.ComplaintCases
                .AsNoTracking()
                .OrderByDescending(item => item.CreatedAtUtc)
                .Take(5)
                .ToListAsync();
        }

        var viewModel = new HomeIndexViewModel
        {
            TotalPosts = await _dbContext.Posts.CountAsync(),
            TotalComments = await _dbContext.Comments.CountAsync(),
            NoticeCount = noticeCount,
            LatestPostTitle = recentPosts.FirstOrDefault()?.Title,
            RecentPosts = recentPosts,
            MostLikedPosts = mostLikedPosts,
            Notices = notices,
            FeaturedNotice = featuredNotice,
            IsAdmin = isAdmin,
            LostFoundOpenCount = lostFoundOpenCount,
            LostFoundUnderReviewCount = lostFoundUnderReviewCount,
            LostFoundResolvedCount = lostFoundResolvedCount,
            LostFoundClaimsLast7Days = lostFoundClaimsLast7Days,
            RecentLostFoundItems = recentLostFoundItems,
            ComplaintOpenCount = complaintOpenCount,
            ComplaintCompletedCount = complaintCompletedCount,
            RecentComplaints = recentComplaints
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
