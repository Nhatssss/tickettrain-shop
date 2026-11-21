using System.Data;
using Microsoft.Data.SqlClient;
using TrainTicketWeb.Models;

using TrainTicketWeb.Data;

namespace TrainTicketWeb.Repositories;

public interface IStationRepository
{
    List<Station> GetAll(bool activeOnly = false);
    Station GetById(int id);
    void Save(Station station);
}

public class StationRepository : IStationRepository
{
    public List<Station> GetAll(bool activeOnly = false)
        => Db.Map<Station>(Db.Query("SELECT * FROM stations"
            + (activeOnly ? " WHERE is_active = 1" : "") + " ORDER BY station_id"));

    public Station GetById(int id)
        => Db.MapOne<Station>(Db.Query("SELECT * FROM stations WHERE station_id = @id", Db.P("@id", id)));

    public void Save(Station s)
    {
        if (s.StationId == 0)
            Db.Execute("INSERT INTO stations (station_code, station_name, province, is_active) VALUES (@c,@n,@p,@a)",
                Db.P("@c", s.StationCode.Trim()), Db.P("@n", s.StationName.Trim()),
                Db.P("@p", (s.Province ?? "").Trim()), Db.P("@a", s.IsActive));
        else
            Db.Execute("UPDATE stations SET station_code=@c, station_name=@n, province=@p, is_active=@a WHERE station_id=@id",
                Db.P("@c", s.StationCode.Trim()), Db.P("@n", s.StationName.Trim()),
                Db.P("@p", (s.Province ?? "").Trim()), Db.P("@a", s.IsActive), Db.P("@id", s.StationId));
    }
}
