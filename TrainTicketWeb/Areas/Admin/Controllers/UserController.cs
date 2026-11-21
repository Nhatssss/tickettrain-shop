using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;

namespace TrainTicketWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class UserController : Controller
{
    private readonly IUserService _users;

    public UserController(IUserService users) => _users = users;

    public IActionResult Users() => View(_users.GetAll());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetUserStatus(int userId, string status)
    {
        _users.SetStatus(userId, status);
        TempData["Success"] = "Đã cập nhật trạng thái người dùng.";
        return RedirectToAction(nameof(Users));
    }
}
