using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;

namespace TrainTicketWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class BookingController : Controller
{
    private readonly IBookingService _bookings;

    public BookingController(IBookingService bookings) => _bookings = bookings;

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
}
