using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;

namespace TrainTicketWeb.Services;

public interface IAuthService
{
    User GetByEmail(string email);
    User GetById(int id);
    (bool ok, string msg) Register(string fullName, string email, string phone, string password, string confirm);
    User ValidateLogin(string email, string password);
    Task SignIn(HttpContext ctx, User u, bool remember);
    Task SignOut(HttpContext ctx);
    void ChangePassword(int userId, string newPassword);
    void UpdateProfile(int userId, string fullName, string phone);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;

    public AuthService(IUserRepository users) => _users = users;

    public User GetByEmail(string email) => _users.GetByEmail((email ?? "").Trim());

    public User GetById(int id) => _users.GetById(id);

    public (bool ok, string msg) Register(string fullName, string email, string phone, string password, string confirm)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return (false, "Vui lòng nhập họ tên.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@")) return (false, "Email không hợp lệ.");
        if (string.IsNullOrEmpty(password) || password.Length < 6) return (false, "Mật khẩu phải từ 6 ký tự trở lên.");
        if (password != confirm) return (false, "Mật khẩu nhập lại không khớp.");
        if (GetByEmail(email.Trim()) != null) return (false, "Email đã được sử dụng.");

        _users.Insert(new User
        {
            FullName = fullName.Trim(),
            Email = email.Trim(),
            Phone = (phone ?? "").Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            Role = "customer",
            Status = "active"
        });
        return (true, "Đăng ký thành công.");
    }

    public User ValidateLogin(string email, string password)
    {
        var u = GetByEmail(email);
        if (u == null) return null;
        if (u.Status == "locked") return null;
        bool ok;
        try { ok = BCrypt.Net.BCrypt.Verify(password ?? "", u.PasswordHash); }
        catch { ok = false; }
        return ok ? u : null;
    }

    public async Task SignIn(HttpContext ctx, User u, bool remember)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, u.UserId.ToString()),
            new Claim(ClaimTypes.Name, u.Email),
            new Claim(ClaimTypes.GivenName, u.FullName),
            new Claim(ClaimTypes.Role, u.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = remember, ExpiresUtc = DateTime.UtcNow.AddHours(8) });
    }

    public async Task SignOut(HttpContext ctx)
        => await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    public void ChangePassword(int userId, string newPassword)
        => _users.UpdatePassword(userId, BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12));

    public void UpdateProfile(int userId, string fullName, string phone)
        => _users.UpdateProfile(userId, fullName, phone);
}
