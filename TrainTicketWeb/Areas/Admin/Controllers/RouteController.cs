using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;
using TrainTicketWeb.ViewModels;

namespace TrainTicketWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class RouteController : Controller
{
    private readonly IRouteService _routes;
    private readonly IStationService _stations;

    public RouteController(IRouteService routes, IStationService stations)
    {
        _routes = routes;
        _stations = stations;
    }

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
}
