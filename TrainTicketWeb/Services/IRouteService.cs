using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;
using Route = TrainTicketWeb.Models.Route;

namespace TrainTicketWeb.Services;

public interface IRouteService
{
    List<Route> GetRoutes();
    Route GetRoute(int id);
    int SaveRoute(int id, string name, int startId, int endId);
    List<RouteStation> GetRouteStations(int routeId);
    void SaveRouteStations(int routeId, List<int> stationIds, List<decimal> distances);
}

public class RouteService : IRouteService
{
    private readonly IRouteRepository _repo;

    public RouteService(IRouteRepository repo) => _repo = repo;

    public List<Route> GetRoutes() => _repo.GetAll();

    public Route GetRoute(int id) => _repo.GetById(id);

    public int SaveRoute(int id, string name, int startId, int endId)
        => _repo.Save(new Route
        {
            RouteId = id,
            RouteName = name ?? "",
            StartStationId = startId,
            EndStationId = endId
        });

    public List<RouteStation> GetRouteStations(int routeId) => _repo.GetStations(routeId);

    public void SaveRouteStations(int routeId, List<int> stationIds, List<decimal> distances)
    {
        if (stationIds == null || distances == null || stationIds.Count != distances.Count || stationIds.Count == 0)
            return;

        var items = new List<RouteStation>();
        for (int i = 0; i < stationIds.Count; i++)
            items.Add(new RouteStation { RouteId = routeId, StationId = stationIds[i], DistanceKm = distances[i] });

        _repo.SaveStations(routeId, items);
    }
}
