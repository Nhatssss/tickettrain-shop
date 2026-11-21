using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;

namespace TrainTicketWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class CoachTypeController : Controller
{
    private readonly ITrainService _trains;

    public CoachTypeController(ITrainService trains) => _trains = trains;

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
}
