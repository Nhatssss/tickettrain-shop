using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Repositories;
using TrainTicketWeb.ViewModels;

namespace TrainTicketWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class DashboardController : Controller
{
    private readonly IReportRepository _reports;

    public DashboardController(IReportRepository reports) => _reports = reports;

    public IActionResult Index()
    {
        var vm = new DashboardViewModel
        {
            TotalRevenue = _reports.TotalRevenue(),
            RevenueLast7 = _reports.RevenueBetween(DateTime.Today.AddDays(-6), DateTime.Today.AddDays(1)),
            RevenueLast30 = _reports.RevenueBetween(DateTime.Today.AddDays(-29), DateTime.Today.AddDays(1)),
            TotalBookings = _reports.CountBookings(),
            TotalTickets = _reports.CountPaidTickets(),
            TotalCustomers = _reports.CountCustomers(),
            TotalTrips = _reports.CountTrips(),
            PendingRefunds = _reports.CountPendingRefunds(),
            RevenueByRoute = _reports.RevenueByRoute(),
            RevenueByDay = _reports.RevenueByDay(14),
            OccupancyByTrip = _reports.OccupancyByTrip()
        };
        return View(vm);
    }
}
