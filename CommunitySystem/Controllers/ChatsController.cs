using CommunitySystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunitySystem.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.User}")]
public class ChatsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

