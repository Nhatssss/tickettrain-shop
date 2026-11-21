using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;

namespace TrainTicketWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class TrainController : Controller
{
    private readonly ITrainService _trains;
    private readonly IRouteService _routes;

    public TrainController(ITrainService trains, IRouteService routes)
    {
        _trains = trains;
        _routes = routes;
    }

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
}
