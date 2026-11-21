using System.Data;
using Microsoft.Data.SqlClient;
using TrainTicketWeb.Models;
using Route = TrainTicketWeb.Models.Route;

using TrainTicketWeb.Data;

namespace TrainTicketWeb.Repositories;

public interface IRouteRepository
{
    List<Route> GetAll();
    Route GetById(int id);
    int Save(Route route);
    List<RouteStation> GetStations(int routeId);
    void SaveStations(int routeId, List<RouteStation> items);
}

public class RouteRepository : IRouteRepository
{
    public List<Route> GetAll()
        => Db.Map<Route>(Db.Query(@"
            SELECT r.*, s1.station_name AS StartStationName, s2.station_name AS EndStationName
            FROM routes r
            JOIN stations s1 ON s1.station_id = r.start_station_id
            JOIN stations s2 ON s2.station_id = r.end_station_id
            ORDER BY r.route_id"));

    public Route GetById(int id)
        => Db.MapOne<Route>(Db.Query(@"
            SELECT r.*, s1.station_name AS StartStationName, s2.station_name AS EndStationName
            FROM routes r
            JOIN stations s1 ON s1.station_id = r.start_station_id
            JOIN stations s2 ON s2.station_id = r.end_station_id
            WHERE r.route_id = @id", Db.P("@id", id)));

    public int Save(Route route)
    {
        if (route.RouteId == 0)
        {
            Db.Execute("INSERT INTO routes (route_name, start_station_id, end_station_id) VALUES (@n,@s,@e)",
                Db.P("@n", route.RouteName.Trim()), Db.P("@s", route.StartStationId), Db.P("@e", route.EndStationId));
            return Convert.ToInt32(Db.Scalar("SELECT SCOPE_IDENTITY()"));
        }
        Db.Execute("UPDATE routes SET route_name=@n, start_station_id=@s, end_station_id=@e WHERE route_id=@id",
            Db.P("@n", route.RouteName.Trim()), Db.P("@s", route.StartStationId),
            Db.P("@e", route.EndStationId), Db.P("@id", route.RouteId));
        return route.RouteId;
    }

    public List<RouteStation> GetStations(int routeId)
        => Db.Map<RouteStation>(Db.Query(@"
            SELECT rs.*, s.station_name AS StationName
            FROM route_stations rs JOIN stations s ON s.station_id = rs.station_id
            WHERE rs.route_id = @id ORDER BY rs.stop_order", Db.P("@id", routeId)));

    public void SaveStations(int routeId, List<RouteStation> items)
    {
        Db.InTransaction<object>((c, tx) =>
        {
            var del = new SqlCommand("DELETE FROM route_stations WHERE route_id = @r", c, tx);
            del.Parameters.AddWithValue("@r", routeId);
            del.ExecuteNonQuery();

            int order = 1;
            foreach (var item in items)
            {
                var ins = new SqlCommand(
                    "INSERT INTO route_stations (route_id, station_id, stop_order, distance_km) VALUES (@r,@s,@o,@d)", c, tx);
                ins.Parameters.AddWithValue("@r", routeId);
                ins.Parameters.AddWithValue("@s", item.StationId);
                ins.Parameters.AddWithValue("@o", order++);
                ins.Parameters.AddWithValue("@d", item.DistanceKm);
                ins.ExecuteNonQuery();
            }
            return null;
        });
    }
}
