using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;

namespace TrainTicketWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class RefundController : Controller
{
    private readonly IBookingService _bookings;

    public RefundController(IBookingService bookings) => _bookings = bookings;

    private int AdminId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

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
}
