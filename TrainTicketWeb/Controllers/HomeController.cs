using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Models;
using TrainTicketWeb.Services;
using TrainTicketWeb.ViewModels;

namespace TrainTicketWeb.Controllers;

public class HomeController : Controller
{
    private readonly IStationService _stations;
    private readonly IScheduleService _schedules;

    public HomeController(IStationService stations, IScheduleService schedules)
    {
        _stations = stations;
        _schedules = schedules;
    }

    public IActionResult Index()
    {
        var vm = new SearchViewModel
        {
            DepartureDate = DateTime.Today.AddDays(1),
            Stations = _stations.GetStations(activeOnly: true)
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Search(SearchViewModel q)
    {
        q ??= new SearchViewModel();
        q.Stations = _stations.GetStations(activeOnly: true);

        if (q.FromStationId <= 0 || q.ToStationId <= 0)
            q.Error = "Vui lòng chọn ga đi và ga đến.";
        else if (q.FromStationId == q.ToStationId)
            q.Error = "Ga đi và ga đến phải khác nhau.";
        else if (!q.DepartureDate.HasValue)
            q.Error = "Vui lòng chọn ngày đi.";
        else if (q.DepartureDate.Value.Date < DateTime.Today)
            q.Error = "Ngày đi không được ở quá khứ.";
        else if (q.TripType == "round_trip" && (!q.ReturnDate.HasValue || q.ReturnDate.Value.Date < q.DepartureDate.Value.Date))
            q.Error = "Ngày về không hợp lệ (phải từ ngày đi trở đi).";
        else if (q.TotalPassengers <= 0)
            q.Error = "Số hành khách phải lớn hơn 0.";

        var vm = new SearchResultsViewModel { Query = q };
        vm.FromStationName = _stations.GetStation(q.FromStationId)?.StationName;
        vm.ToStationName = _stations.GetStation(q.ToStationId)?.StationName;

        if (string.IsNullOrEmpty(q.Error))
        {
            vm.Outbound = _schedules.Search(q.FromStationId, q.ToStationId, q.DepartureDate.Value);
            if (q.TripType == "round_trip")
                vm.Return = _schedules.Search(q.ToStationId, q.FromStationId, q.ReturnDate.Value);
        }
        return View(vm);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
