using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Services;
using TrainTicketWeb.ViewModels;

namespace TrainTicketWeb.ViewComponents;

public class StationMenuViewComponent : ViewComponent
{
    private readonly IStationService _stations;

    public StationMenuViewComponent(IStationService stations) => _stations = stations;

    public IViewComponentResult Invoke(int currentStationId = 0)
    {
        var vm = new StationMenuViewModel
        {
            Stations = _stations.GetStations(activeOnly: true),
            CurrentStationId = currentStationId
        };
        return View(vm);
    }
}
