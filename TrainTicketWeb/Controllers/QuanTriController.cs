using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Data;
using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;
using TrainTicketWeb.Services;
using TrainTicketWeb.ViewModels;

namespace TrainTicketWeb.Controllers;

[Authorize(Roles = "admin")]
public class QuanTriController : Controller
{
    private readonly IReportRepository _reports;
    private readonly IStationService _stations;
    private readonly IRouteService _routes;
    private readonly ITrainService _trains;
    private readonly IScheduleService _schedules;
    private readonly IBookingService _bookings;
    private readonly IUserService _users;
    private readonly ICouponService _coupons;
    private readonly ITripAdminService _tripAdmin;
    private readonly IFinanceRepository _finance;
    private readonly INotificationRepository _notes;

    public QuanTriController(IReportRepository reports, IStationService stations,
        IRouteService routes, ITrainService trains, IScheduleService schedules,
        IBookingService bookings, IUserService users, ICouponService coupons,
        ITripAdminService tripAdmin, IFinanceRepository finance, INotificationRepository notes)
    {
        _reports = reports;
        _stations = stations;
        _routes = routes;
        _trains = trains;
        _schedules = schedules;
        _bookings = bookings;
        _users = users;
        _coupons = coupons;
        _tripAdmin = tripAdmin;
        _finance = finance;
        _notes = notes;
    }

    private int AdminId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

    // ===== Tổng quan =====
    public IActionResult Index()
    {
        var vm = new DashboardViewModel
        {
            TotalRevenue = _reports.TotalRevenue(),
            RevenueLast7 = _reports.RevenueBetween(DateTime.Today.AddDays(-6), DateTime.Today.AddDays(1)),
            RevenueLast30 = _reports.RevenueBetween(DateTime.Today.AddDays(-29), DateTime.Today.AddDays(1)),
            TotalBookings = _reports.CountBookings(),
            TotalTickets = _reports.CountPaidTickets(),
            TotalCustomers = _reports.CountCustomers(),
            TotalTrips = _reports.CountTrips(),
            PendingRefunds = _reports.CountPendingRefunds(),
            RevenueByRoute = _reports.RevenueByRoute(),
            RevenueByDay = _reports.RevenueByDay(14),
            OccupancyByTrip = _reports.OccupancyByTrip()
        };
        return View("Dashboard", vm);
    }

    // ===== Ga tàu =====
    public IActionResult Stations() => View(_stations.GetStations());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveStation(int id, string station_code, string station_name, string province, bool is_active)
    {
        _stations.SaveStation(id, station_code, station_name, province, is_active);
        TempData["Success"] = "Đã lưu ga tàu.";
        return RedirectToAction(nameof(Stations));
    }

    // ===== Tuyến đường =====
    public IActionResult Routes() => View(_routes.GetRoutes());

    public IActionResult RouteDetail(int id)
    {
        var route = _routes.GetRoute(id);
        if (route == null) return NotFound();

        return View(new RouteDetailViewModel
        {
            Route = route,
            RouteStations = _routes.GetRouteStations(id),
            AllStations = _stations.GetStations(activeOnly: true)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveRoute(int id, string route_name, int start_station_id, int end_station_id)
    {
        int rid = _routes.SaveRoute(id, route_name, start_station_id, end_station_id);
        TempData["Success"] = "Đã lưu tuyến đường.";
        return RedirectToAction(nameof(RouteDetail), new { id = rid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveRouteStations(int route_id, List<int> station_id, List<decimal> distance_km)
    {
        if (station_id != null && distance_km != null && station_id.Count == distance_km.Count && station_id.Count > 0)
        {
            _routes.SaveRouteStations(route_id, station_id, distance_km);
            TempData["Success"] = "Đã cập nhật danh sách ga của tuyến.";
        }
        else
        {
            TempData["Error"] = "Danh sách ga không hợp lệ.";
        }
        return RedirectToAction(nameof(RouteDetail), new { id = route_id });
    }

    // ===== Đoàn tàu =====
    public IActionResult Trains()
    {
        ViewBag.Routes = _routes.GetRoutes();
        return View(_trains.GetTrains());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveTrain(int id, string train_code, int route_id, bool is_active)
    {
        _trains.SaveTrain(id, train_code, route_id, is_active);
        TempData["Success"] = "Đã lưu đoàn tàu.";
        return RedirectToAction(nameof(Trains));
    }

    // ===== Cấu hình toa/ghế =====
    public IActionResult Coaches(int trainId)
    {
        var train = _trains.GetTrain(trainId);
        if (train == null) return NotFound();

        ViewBag.Train = train;
        ViewBag.CoachTypes = _trains.GetCoachTypes();
        ViewBag.Coaches = _trains.GetCoaches(trainId);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfigureTrain(int trainId, List<int> coachTypeId, List<int> coachCount)
    {
        if (coachTypeId != null && coachCount != null && coachTypeId.Count == coachCount.Count)
        {
            var config = new List<(int, int)>();
            for (int i = 0; i < coachTypeId.Count; i++)
                if (coachCount[i] > 0) config.Add((coachTypeId[i], coachCount[i]));

            if (config.Count > 0)
            {
                _trains.ConfigureTrain(trainId, config);
                TempData["Success"] = "Đã cấu hình lại toa và ghế cho đoàn tàu.";
            }
            else
            {
                TempData["Error"] = "Chưa chọn số toa nào.";
            }
        }
        return RedirectToAction(nameof(Coaches), new { trainId });
    }

    // ===== Chuyến tàu =====
    public IActionResult Trips()
    {
        ViewBag.Trains = _trains.GetTrains();
        return View(_schedules.GetTrips());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateTrip(int trainId, DateTime date, string time)
    {
        if (!TimeSpan.TryParse(time, out var baseTime)) baseTime = new TimeSpan(19, 0, 0);
        _schedules.CreateTrip(trainId, date, baseTime);
        TempData["Success"] = "Đã thêm chuyến tàu.";
        return RedirectToAction(nameof(Trips));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateTripBatch(int trainId, DateTime fromDate, DateTime toDate, string time)
    {
        if (!TimeSpan.TryParse(time, out var baseTime)) baseTime = new TimeSpan(19, 0, 0);
        int count = 0;
        for (var d = fromDate.Date; d <= toDate.Date; d = d.AddDays(1))
        {
            _schedules.CreateTrip(trainId, d, baseTime);
            count++;
        }
        TempData["Success"] = $"Đã thêm {count} chuyến tàu.";
        return RedirectToAction(nameof(Trips));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetTripStatus(int tripId, string status)
    {
        _schedules.SetTripStatus(tripId, status);
        TempData["Success"] = "Đã cập nhật trạng thái chuyến.";
        return RedirectToAction(nameof(Trips));
    }

    // ===== Bảng giá =====
    public IActionResult Fares()
    {
        ViewBag.Stations = _stations.GetStations(activeOnly: true);
        ViewBag.CoachTypes = _trains.GetCoachTypes();
        return View(_schedules.GetFares());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveFare(int id, int fromId, int toId, int coachTypeId, decimal base_price,
        DateTime effective_from, DateTime? effective_to)
    {
        if (fromId == toId)
        {
            TempData["Error"] = "Ga đi và ga đến không được trùng nhau.";
            return RedirectToAction(nameof(Fares));
        }
        _schedules.SaveFare(id, fromId, toId, coachTypeId, base_price, effective_from, effective_to);
        TempData["Success"] = "Đã lưu giá vé.";
        return RedirectToAction(nameof(Fares));
    }

    // ===== Đối tượng khách =====
    public IActionResult PassengerTypes() => View(_schedules.GetPassengerTypes());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SavePassengerType(int id, string type_name, decimal discount_percent,
        int? min_age, int? max_age, string id_label, bool is_active)
    {
        _schedules.SavePassengerType(id, type_name, discount_percent, min_age, max_age, id_label, is_active);
        TempData["Success"] = "Đã lưu đối tượng hành khách.";
        return RedirectToAction(nameof(PassengerTypes));
    }

    // ===== Đơn hàng =====
    public IActionResult Bookings(string status = null)
    {
        ViewBag.Status = status;
        return View(_bookings.GetAll(status));
    }

    public IActionResult BookingDetail(int bookingId)
    {
        var vm = _bookings.GetBookingDetail(bookingId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ===== Hoàn tiền =====
    public IActionResult Refunds() => View(_bookings.GetRefunds());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ProcessRefund(int refundId, string decision)
    {
        bool approve = decision == "approve";
        _bookings.ProcessRefund(refundId, AdminId, approve);
        TempData["Success"] = approve ? "Đã duyệt hoàn tiền." : "Đã từ chối hoàn tiền.";
        return RedirectToAction(nameof(Refunds));
    }

    // ===== Người dùng =====
    public IActionResult Users() => View(_users.GetAll());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetUserStatus(int userId, string status)
    {
        _users.SetStatus(userId, status);
        TempData["Success"] = "Đã cập nhật trạng thái người dùng.";
        return RedirectToAction(nameof(Users));
    }

    // ===== Quản lý mã giảm giá =====
    public IActionResult Coupons()
    {
        ViewBag.ActiveCount = _coupons.GetActive().Count;
        ViewBag.DiscountAll = _coupons.GetAll().Sum(c => 0m);
        return View(_coupons.GetAll());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveCoupon(CouponFormViewModel form)
    {
        var (ok, error, id) = _coupons.Save(form);
        TempData[ok ? "Success" : "Error"] = ok
            ? $"Đã lưu mã giảm giá {(form.CouponId > 0 ? "#" + form.CouponId : "mới")}."
            : error;
        return RedirectToAction(nameof(Coupons));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetCouponActive(int id, bool active)
    {
        _coupons.SetActive(id, active);
        TempData["Success"] = active ? "Đã bật mã giảm giá." : "Đã tạm dừng mã giảm giá.";
        return RedirectToAction(nameof(Coupons));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteCoupon(int id)
    {
        var c = _coupons.GetAll().FirstOrDefault(x => x.CouponId == id);
        if (c != null && c.UsedCount > 0)
        {
            TempData["Error"] = $"Mã {c.Code} đã dùng {c.UsedCount} lần nên không xóa được. Hãy tạm dừng mã.";
            return RedirectToAction(nameof(Coupons));
        }
        _coupons.Delete(id);
        TempData["Success"] = "Đã xóa mã giảm giá.";
        return RedirectToAction(nameof(Coupons));
    }

    // ===== Chi tiết chuyến / hoãn / hủy =====
    public IActionResult TripDetail(int id)
    {
        var vm = _tripAdmin.GetDetail(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DelayTrip(int tripId, int minutes, string note)
    {
        var (ok, msg, notified) = _tripAdmin.DelayTrip(tripId, minutes, note);
        TempData[ok ? "Success" : "Error"] = ok
            ? $"{msg} Đã gửi thông báo tới {notified} khách."
            : msg;
        return RedirectToAction(nameof(TripDetail), new { id = tripId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ClearDelay(int tripId)
    {
        var (ok, msg) = _tripAdmin.ClearDelay(tripId);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(TripDetail), new { id = tripId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CancelTrip(int tripId, string reason)
    {
        var (ok, msg, refunded, amount, notified) = _tripAdmin.CancelTrip(tripId, reason, AdminId);
        TempData[ok ? "Success" : "Error"] = ok
            ? $"{msg} Đã thông báo tới {notified} khách."
            : msg;
        return RedirectToAction(nameof(TripDetail), new { id = tripId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteTrip(int tripId)
    {
        var (ok, msg) = _tripAdmin.DeleteTrip(tripId);
        TempData[ok ? "Success" : "Error"] = msg;
        return ok ? RedirectToAction(nameof(Trips)) : RedirectToAction(nameof(TripDetail), new { id = tripId });
    }

    // ===== Quỹ của admin + số tiền từng khách =====
    public IActionResult Finance()
    {
        var vm = new FinancePageViewModel
        {
            Summary = _finance.GetSummary(),
            Customers = _finance.GetCustomerList()
        };
        return View(vm);
    }

    // ===== Thông báo đã phát (admin xem lại) =====
    public IActionResult Notifications()
    {
        var dt = Db.Query(@"
            SELECT n.notification_id, n.type, n.title, n.body, n.is_read, n.created_at,
                   u.email AS UserEmail, u.full_name AS FullName
            FROM notifications n
            JOIN users u ON u.user_id = n.user_id
            ORDER BY n.created_at DESC");
        ViewBag.Rows = dt;
        return View();
    }
}
