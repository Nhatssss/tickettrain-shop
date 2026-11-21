using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Data;
using TrainTicketWeb.Models;
using TrainTicketWeb.Services;
using TrainTicketWeb.ViewModels;

namespace TrainTicketWeb.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly IBookingService _bookings;
    private readonly IScheduleService _schedules;
    private readonly IStationService _stations;
    private readonly ITrainService _trains;
    private readonly IUserService _users;
    private readonly ICouponService _coupons;

    public BookingController(IBookingService bookings, IScheduleService schedules,
        IStationService stations, ITrainService trains, IUserService users, ICouponService coupons)
    {
        _bookings = bookings;
        _schedules = schedules;
        _stations = stations;
        _trains = trains;
        _users = users;
        _coupons = coupons;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    // ===== Chọn chỗ =====
    [HttpGet]
    public IActionResult SelectSeats(int tripId, int fromId, int toId, string leg = "outbound", int passengerCount = 1)
    {
        var trip = _schedules.GetTrip(tripId);
        if (trip == null) return RedirectToAction("Index", "Home");

        var vm = new SeatSelectViewModel
        {
            TripId = tripId,
            TrainCode = trip.TrainCode,
            RouteName = trip.RouteName,
            DepartureDate = trip.DepartureDate,
            FromStationId = fromId,
            ToStationId = toId,
            FromStationName = _stations.GetStation(fromId)?.StationName,
            ToStationName = _stations.GetStation(toId)?.StationName,
            Leg = leg,
            PassengerCount = passengerCount,
            Coaches = _bookings.GetSeatMap(tripId, fromId, toId)
        };
        return View(vm);
    }

    // ===== Giữ chỗ (POST) =====
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Hold(int tripId, int fromId, int toId, string leg, int passengerCount, List<int> seatIds)
    {
        if (seatIds == null || seatIds.Count == 0)
        {
            TempData["Error"] = "Vui lòng chọn ít nhất một chỗ.";
            return RedirectToAction("SelectSeats", new { tripId, fromId, toId, leg, passengerCount });
        }
        if (seatIds.Count != passengerCount)
        {
            TempData["Error"] = $"Bạn cần chọn đúng {passengerCount} chỗ (đã chọn {seatIds.Count}).";
            return RedirectToAction("SelectSeats", new { tripId, fromId, toId, leg, passengerCount });
        }

        var user = _users.GetById(CurrentUserId);
        var picks = new List<SeatPick>();
        foreach (var sid in seatIds)
        {
            picks.Add(new SeatPick
            {
                TripId = tripId,
                SeatId = sid,
                FromId = fromId,
                ToId = toId,
                Leg = leg,
                CoachTypeId = _trains.GetCoachTypeOfSeat(sid)
            });
        }

        var (bookingId, error) = _bookings.CreateBooking(CurrentUserId, "one_way",
            user?.Email ?? "", user?.Phone ?? "0000000000", picks);

        if (bookingId == 0)
        {
            TempData["Error"] = error;
            return RedirectToAction("SelectSeats", new { tripId, fromId, toId, leg, passengerCount });
        }
        return RedirectToAction("Passengers", new { bookingId });
    }

    // ===== Nhập thông tin hành khách =====
    [HttpGet]
        [Route("Booking/Passengers/{bookingId:int}")]
public IActionResult Passengers(int bookingId)
    {
        var b = _bookings.GetBooking(bookingId);
        if (b == null || b.UserId != CurrentUserId) return RedirectToAction("MyBookings");

        var vm = new PassengersViewModel
        {
            BookingId = bookingId,
            BookingCode = b.BookingCode,
            TripType = b.TripType,
            Passengers = _bookings.GetBookingPassengers(bookingId),
            PassengerTypes = _schedules.GetPassengerTypes(activeOnly: true)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Passengers(int bookingId, List<int> ticketIds, List<string> names,
        List<string> idNumbers, List<int> passengerTypeIds)
    {
        var b = _bookings.GetBooking(bookingId);
        if (b == null || b.UserId != CurrentUserId) return RedirectToAction("MyBookings");

        if (ticketIds == null) ticketIds = new List<int>();

        var rows = new List<PassengerRow>();
        for (int i = 0; i < ticketIds.Count; i++)
        {
            string name = (names != null && i < names.Count) ? names[i] : "";
            string idNo = (idNumbers != null && i < idNumbers.Count) ? idNumbers[i] : "";
            int typeId = (passengerTypeIds != null && i < passengerTypeIds.Count) ? passengerTypeIds[i] : 0;

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ họ tên hành khách.";
                return RedirectToAction("Passengers", new { bookingId });
            }
            var pt = _schedules.GetPassengerType(typeId);
            if (pt != null && !string.IsNullOrEmpty(pt.IdLabel) && string.IsNullOrWhiteSpace(idNo))
            {
                TempData["Error"] = $"Đối tượng '{pt.TypeName}' bắt buộc nhập {pt.IdLabel}.";
                return RedirectToAction("Passengers", new { bookingId });
            }
            rows.Add(new PassengerRow
            {
                TicketId = ticketIds[i],
                PassengerName = name,
                IdNumber = idNo ?? "",
                PassengerTypeId = typeId
            });
        }

        _bookings.UpdatePassengers(bookingId, rows);
        return RedirectToAction("Checkout", new { bookingId });
    }

    // ===== Thanh toán =====
    [HttpGet]
        [Route("Booking/Checkout/{bookingId:int}")]
public IActionResult Checkout(int bookingId)
    {
        var b = _bookings.GetBooking(bookingId);
        if (b == null || b.UserId != CurrentUserId) return RedirectToAction("MyBookings");
        if (b.Status != "holding") return RedirectToAction("Detail", new { bookingId });
        return View(_bookings.GetBookingDetail(bookingId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Pay(int bookingId, string method)
    {
        var b = _bookings.GetBooking(bookingId);
        if (b == null || b.UserId != CurrentUserId) return RedirectToAction("MyBookings");

        var valid = new[] { "vnpay", "momo", "bank_qr" };
        if (!valid.Contains(method)) method = "bank_qr";

        bool ok = _bookings.Pay(bookingId, method);
        if (!ok)
        {
            TempData["Error"] = "Đơn đã hết hạn giữ chỗ hoặc không ở trạng thái thanh toán.";
            return RedirectToAction("MyBookings");
        }
        return RedirectToAction("Success", new { bookingId });
    }

    [Route("Booking/Success/{bookingId:int}")]
    public IActionResult Success(int bookingId)
    {
        var vm = _bookings.GetBookingDetail(bookingId);
        if (vm == null || vm.Booking.UserId != CurrentUserId) return RedirectToAction("MyBookings");
        if (vm.Booking.Status == "paid" && !vm.Booking.CouponId.HasValue)
            TempData["CouponHint"] = "Bạn chưa dùng mã giảm giá cho đơn này.";
        return View(vm);
    }

    // ===== Áp / bỏ mã giảm giá (AJAX ở trang thanh toán) =====
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApplyCoupon(int bookingId, string code)
    {
        var b = _bookings.GetBooking(bookingId);
        if (b == null || b.UserId != CurrentUserId)
            return Json(new { ok = false, error = "Không tìm thấy đơn hàng.", discount = 0, subtotal = 0, total = 0 });
        if (b.Status != "holding")
            return Json(new { ok = false, error = "Đơn đã thanh toán hoặc đã hết hạn giữ chỗ.", discount = 0, subtotal = 0, total = 0 });

        var r = _coupons.Apply(code, CurrentUserId, bookingId);
        if (!r.Ok)
            return Json(new { ok = false, error = r.Error, discount = 0, subtotal = r.Subtotal, total = r.Subtotal });

        var coupon = _coupons.GetByCode(r.Code);
        Db.Execute(@"UPDATE bookings SET coupon_id=@c, discount_amount=@d, total_amount=@t
                          WHERE booking_id=@b AND [status]='holding'",
            Db.P("@c", coupon?.CouponId), Db.P("@d", r.Discount),
            Db.P("@t", r.Total), Db.P("@b", bookingId));

        return Json(new { ok = true, code = r.Code, discount = r.Discount, subtotal = r.Subtotal, total = r.Total });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Booking/ClearCoupon/{bookingId:int}")]
    public IActionResult ClearCoupon(int bookingId)
    {
        var b = _bookings.GetBooking(bookingId);
        if (b == null || b.UserId != CurrentUserId)
            return Json(new { ok = false, discount = 0, subtotal = 0, total = 0 });

        decimal subtotal = _coupons.Subtotal(bookingId);
        Db.Execute(@"UPDATE bookings SET coupon_id=NULL, discount_amount=0, total_amount=@t
                          WHERE booking_id=@b AND [status]='holding'",
            Db.P("@t", subtotal), Db.P("@b", bookingId));
        return Json(new { ok = true, discount = 0, subtotal = subtotal, total = subtotal });
    }

    // ===== Lịch sử =====
    public IActionResult MyBookings() => View(_bookings.GetMyBookings(CurrentUserId));

    [Route("Booking/Detail/{bookingId:int}")]
    public IActionResult Detail(int bookingId)
    {
        var vm = _bookings.GetBookingDetail(bookingId);
        if (vm == null || vm.Booking.UserId != CurrentUserId) return RedirectToAction("MyBookings");
        return View(vm);
    }

    [Route("Booking/Ticket/{ticketId:int}")]
    public IActionResult Ticket(int ticketId)
    {
        var t = _bookings.GetTicketDetail(ticketId);
        if (t == null) return RedirectToAction("MyBookings");
        var b = _bookings.GetBooking(t.Ticket.BookingId);
        if (b == null || b.UserId != CurrentUserId) return RedirectToAction("MyBookings");
        ViewBag.BookingCode = b.BookingCode;
        return View(t);
    }

    // ===== Hủy vé =====
    [HttpGet]
        [Route("Booking/Cancel/{ticketId:int}")]
public IActionResult Cancel(int ticketId)
    {
        var t = _bookings.GetTicketDetail(ticketId);
        if (t == null) return RedirectToAction("MyBookings");
        var b = _bookings.GetBooking(t.Ticket.BookingId);
        if (b == null || b.UserId != CurrentUserId) return RedirectToAction("MyBookings");

        var depart = _schedules.GetDepartureTime(t.Ticket.TripId, t.Ticket.FromStationId) ?? DateTime.Now.AddHours(1);
        double hours = (depart - DateTime.Now).TotalHours;
        decimal feePct = AppSettings.GetCancelFeePercent(hours);
        decimal fee = Math.Round(t.Ticket.FinalPrice * feePct / 100m, 0);

        ViewBag.Fee = fee;
        ViewBag.Refund = t.Ticket.FinalPrice - fee;
        ViewBag.HoursBefore = hours;
        return View(t);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Booking/CancelConfirmed/{ticketId:int}")]
    public IActionResult CancelConfirmed(int ticketId)
    {
        var (ok, msg, fee, refund) = _bookings.RequestRefund(ticketId, CurrentUserId);
        if (!ok) TempData["Error"] = msg;
        else TempData["Success"] = $"Yêu cầu hủy vé đã được gửi. Phí hủy {fee:N0} VNĐ, số tiền dự kiến hoàn {refund:N0} VNĐ.";
        return RedirectToAction("MyBookings");
    }
}
