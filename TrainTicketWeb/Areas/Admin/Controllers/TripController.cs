using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;

namespace TrainTicketWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class TripController : Controller
{
    private readonly IScheduleService _schedules;
    private readonly ITrainService _trains;

    public TripController(IScheduleService schedules, ITrainService trains)
    {
        _schedules = schedules;
        _trains = trains;
    }

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
}
