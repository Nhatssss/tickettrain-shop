using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;

namespace TrainTicketWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class PassengerTypeController : Controller
{
    private readonly IScheduleService _schedules;

    public PassengerTypeController(IScheduleService schedules) => _schedules = schedules;

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
}
