using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TrainTicketWeb.Repositories;

namespace TrainTicketWeb.Filters;

/// <summary>
/// Chặn tài khoản bị khóa (ban) ngay cả khi đang có phiên đăng nhập hợp lệ.
/// Mỗi request đọc lại users.status: nếu 'locked' thì đăng xuất và đá về trang đăng nhập.
/// </summary>
public class BanCheckFilter : IAsyncActionFilter
{
    private readonly IUserRepository _users;

    public BanCheckFilter(IUserRepository users) => _users = users;

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var u = ctx.HttpContext.User;
        if (u?.Identity?.IsAuthenticated == true)
        {
            var idClaim = u.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idClaim, out int userId))
            {
                var dbUser = _users.GetById(userId);
                bool banned = dbUser == null || dbUser.Status == "locked";

                if (banned)
                {
                    // Xoa cookie phien hien tai
                    await ctx.HttpContext.SignOutAsync(
                        Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

                    string msg = dbUser?.LockedReason;
                    if (string.IsNullOrWhiteSpace(msg))
                        msg = "Tài khoản của bạn đã bị khóa bởi quản trị viên.";

                    if (ctx.Controller is Controller c) c.TempData["BannedMessage"] = msg;
                    ctx.Result = new RedirectToActionResult("Login", "Account", null);
                    return;
                }
            }
        }
        await next();
    }
}

/// <summary>
/// Chặn tài khoản admin đi vào nghiệp vụ của khách (đặt vé, giữ chỗ, thanh toán...).
/// Admin chỉ được dùng khu quản trị.
/// </summary>
public class DenyAdminFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext ctx)
    {
        if (ctx.HttpContext.User?.IsInRole("admin") == true)
        {
            if (ctx.Controller is Controller c)
                c.TempData["Error"] = "Tài khoản quản trị không sử dụng nghiệp vụ đặt vé. Vui lòng dùng tài khoản khách hàng.";
            ctx.Result = new RedirectToActionResult("Index", "QuanTri", null);
        }
    }

    public void OnActionExecuted(ActionExecutedContext ctx) { }
}
