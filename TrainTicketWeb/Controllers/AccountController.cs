using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;

namespace TrainTicketWeb.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly IUserService _users;

    public AccountController(IAuthService auth, IUserService users)
    {
        _auth = auth;
        _users = users;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, bool remember, string returnUrl = null)
    {
        var u = _auth.ValidateLogin(email, password);
        if (u == null)
        {
            ViewBag.Error = "Email hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        await _auth.SignIn(HttpContext, u, remember);
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        // Admin điều hướng về khu quản trị phẳng (quan-tri), không dùng Areas.
        return u.Role == "admin"
            ? RedirectToAction("Index", "QuanTri")
            : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(string fullName, string email, string phone, string password, string confirm)
    {
        var (ok, msg) = _auth.Register(fullName, email, phone, password, confirm);
        if (!ok)
        {
            ViewBag.Error = msg;
            return View();
        }
        TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _auth.SignOut(HttpContext);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public IActionResult Profile() => View(_auth.GetById(CurrentUserId));

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Profile(string fullName, string phone)
    {
        _auth.UpdateProfile(CurrentUserId, fullName, phone);
        TempData["Success"] = "Đã cập nhật thông tin cá nhân.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View();

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(string oldPassword, string newPassword, string confirm)
    {
        var u = _auth.GetById(CurrentUserId);
        bool okOld;
        try { okOld = BCrypt.Net.BCrypt.Verify(oldPassword ?? "", u.PasswordHash); }
        catch { okOld = false; }

        if (!okOld) ViewBag.Error = "Mật khẩu hiện tại không đúng.";
        else if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6) ViewBag.Error = "Mật khẩu mới phải từ 6 ký tự.";
        else if (newPassword != confirm) ViewBag.Error = "Mật khẩu nhập lại không khớp.";
        else
        {
            _auth.ChangePassword(CurrentUserId, newPassword);
            TempData["Success"] = "Đã đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Profile));
        }
        return View();
    }

    public IActionResult AccessDenied() => View();
}
