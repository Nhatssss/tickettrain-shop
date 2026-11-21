using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;

namespace TrainTicketWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class StationController : Controller
{
    private readonly IStationService _stations;

    public StationController(IStationService stations) => _stations = stations;

    public IActionResult Stations() => View(_stations.GetStations());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveStation(int id, string station_code, string station_name, string province, bool is_active)
    {
        _stations.SaveStation(id, station_code, station_name, province, is_active);
        TempData["Success"] = "Đã lưu ga tàu.";
        return RedirectToAction(nameof(Stations));
    }
}
