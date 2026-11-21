using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;

namespace TrainTicketWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class FareController : Controller
{
    private readonly IScheduleService _schedules;
    private readonly IStationService _stations;
    private readonly ITrainService _trains;

    public FareController(IScheduleService schedules, IStationService stations, ITrainService trains)
    {
        _schedules = schedules;
        _stations = stations;
        _trains = trains;
    }

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
}
