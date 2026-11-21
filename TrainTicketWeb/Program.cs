using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using TrainTicketWeb.Data;
using TrainTicketWeb.Filters;
using TrainTicketWeb.Hubs;
using TrainTicketWeb.Repositories;
using TrainTicketWeb.Services;
using TrainTicketWeb.ViewComponents;

var builder = WebApplication.CreateBuilder(args);

// ===== Chay sau IIS (somee.com dung Application Request Routing lam proxy) =====
// IIS la reverse proxy: neu khong doc X-Forwarded-* thi app thay request la http,
// cookie auth se khong duoc set dung va redirect co the lap vo han.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Hosting dung chung: proxy khong nam trong dai mang biet truoc.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllersWithViews(o =>
{
    // Mọi action đều kiểm tra tài khoản có bị khóa hay không
    o.Filters.AddService<BanCheckFilter>();
});

// ===== SignalR: đẩy thông báo real-time xuống trình duyệt khách =====
builder.Services.AddSignalR();

// ===== Tầng dữ liệu + nghiệp vụ (DI) =====
builder.Services.AddScoped<IStationRepository, StationRepository>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<ITrainRepository, TrainRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IFinanceRepository, FinanceRepository>();

builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<ITrainService, TrainService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<ITripAdminService, TripAdminService>();
builder.Services.AddScoped<INotificationPusher, NotificationPusher>();

// ===== Bộ lọc truy cập =====
builder.Services.AddScoped<BanCheckFilter>();
builder.Services.AddScoped<DenyAdminFilter>();

builder.Services.AddScoped<StationMenuViewComponent>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "TrainTicket.Auth";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
    });

var app = builder.Build();

Db.Configure(app.Configuration);
AppSettings.Init(app.Configuration);

// Dat truoc moi middleware khac de Scheme/Host/RemoteIp dung ngay tu dau.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Somee thuong phuc vu qua http. Bat HSTS + redirect cung se gay vong lap
    // khi proxy khong gui X-Forwarded-Proto, nen chi bat khi cau hinh tuong minh.
    if (app.Configuration.GetValue<bool>("Hosting:ForceHttps"))
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "quantri",
    pattern: "quan-tri/{action=Index}/{id?}",
    defaults: new { controller = "QuanTri" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<NotificationHub>("/hub/notifications");

// Trang chan doan cho hosting: kiem tra ngay app co doc duoc DB khong,
// in ro exception that thay vi phai mo log file.
app.MapGet("/Health", (IConfiguration cfg) =>
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("TrainTicketWeb - Health check");
    sb.AppendLine("Environment: " + app.Environment.EnvironmentName);
    sb.AppendLine("ContentRoot: " + app.Environment.ContentRootPath);

    var cs = Environment.GetEnvironmentVariable("ConnectionStrings__TrainTicketDB");
    var src = "env ConnectionStrings__TrainTicketDB";
    if (string.IsNullOrWhiteSpace(cs))
    {
        cs = cfg.GetConnectionString("TrainTicketDB");
        src = "appsettings ConnectionStrings:TrainTicketDB";
    }

    sb.AppendLine("Connection source: " + src);
    if (string.IsNullOrWhiteSpace(cs))
    {
        sb.AppendLine("RESULT: FAIL - khong tim thay chuoi ket noi");
        return Results.Text(sb.ToString(), "text/plain; charset=utf-8");
    }

    // Chi in phan khong bi mat: Server / Database / User. Khong bao gio in password.
    try
    {
        var b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(cs);
        sb.AppendLine("Server: " + b.DataSource);
        sb.AppendLine("Database: " + b.InitialCatalog);
        sb.AppendLine("User: " + b.UserID);
        sb.AppendLine("IntegratedSecurity: " + b.IntegratedSecurity);
    }
    catch (Exception ex)
    {
        sb.AppendLine("CANH BAO: chuoi ket noi sai dinh dang -> " + ex.GetType().Name + ": " + ex.Message);
        return Results.Text(sb.ToString(), "text/plain; charset=utf-8");
    }

    try
    {
        using var c = new Microsoft.Data.SqlClient.SqlConnection(cs);
        c.Open();
        sb.AppendLine("Ket noi: OK");

        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM sys.tables";
            sb.AppendLine("So bang: " + cmd.ExecuteScalar());

            cmd.CommandText = "SELECT COUNT(*) FROM stations";
            sb.AppendLine("So ga: " + cmd.ExecuteScalar());

            cmd.CommandText = "SELECT COUNT(*) FROM seats";
            sb.AppendLine("So ghe: " + cmd.ExecuteScalar());

            cmd.CommandText = "SELECT COUNT(*) FROM users";
            sb.AppendLine("So nguoi dung: " + cmd.ExecuteScalar());
        }

        sb.AppendLine("RESULT: OK");
    }
    catch (Exception ex)
    {
        sb.AppendLine("Ket noi: FAIL");
        sb.AppendLine("Exception: " + ex.GetType().FullName);
        sb.AppendLine("Message: " + ex.Message);
        if (ex.InnerException != null)
            sb.AppendLine("Inner: " + ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
        if (ex is Microsoft.Data.SqlClient.SqlException se)
            sb.AppendLine("SqlErrorNumber: " + se.Number);
        sb.AppendLine("RESULT: FAIL");
    }

    return Results.Text(sb.ToString(), "text/plain; charset=utf-8");
});

app.Run();
