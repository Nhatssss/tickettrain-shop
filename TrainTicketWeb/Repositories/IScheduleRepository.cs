using System.Data;
using TrainTicketWeb.Models;

using TrainTicketWeb.Data;

namespace TrainTicketWeb.Repositories;

public interface IScheduleRepository
{
    Trip GetTrip(int id);
    List<Trip> GetTrips();
    List<TripStop> GetTripStops(int tripId);
    int GetStopOrder(int tripId, int stationId);
    int CreateTrip(int trainId, DateTime date, TimeSpan baseTime);
    void SetTripStatus(int tripId, string status);
    List<Fare> GetFares();
    decimal GetFare(int fromId, int toId, int coachTypeId);
    decimal GetMinFare(int fromId, int toId);
    void SaveFare(Fare fare);
    List<PassengerType> GetPassengerTypes(bool activeOnly = false);
    PassengerType GetPassengerType(int id);
    void SavePassengerType(PassengerType type);
    List<TripSearchResultViewModelData> SearchTrips(int fromId, int toId, DateTime date);
}

/// <summary>Dữ liệu thô trả về từ Repository (chưa map sang ViewModel).</summary>
public class TripSearchResultViewModelData
{
    public int TripId { get; set; }
    public int TrainId { get; set; }
    public string TrainCode { get; set; }
    public int RouteId { get; set; }
    public string RouteName { get; set; }
    public DateTime DepartureDate { get; set; }
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public int AvailableSeats { get; set; }
    public int DelayMinutes { get; set; }
}

public class ScheduleRepository : IScheduleRepository
{
    public Trip GetTrip(int id)
        => Db.MapOne<Trip>(Db.Query(@"
            SELECT tr.*, t.train_code AS TrainCode, r.route_name AS RouteName, r.route_id AS RouteId
            FROM trips tr
            JOIN trains t ON t.train_id = tr.train_id
            JOIN routes r ON r.route_id = t.route_id
            WHERE tr.trip_id = @id", Db.P("@id", id)));

    public List<Trip> GetTrips()
        => Db.Map<Trip>(Db.Query(@"
            SELECT tr.*, t.train_code AS TrainCode, r.route_name AS RouteName, r.route_id AS RouteId
            FROM trips tr
            JOIN trains t ON t.train_id = tr.train_id
            JOIN routes r ON r.route_id = t.route_id
            ORDER BY tr.departure_date DESC, tr.trip_id"));

    public List<TripStop> GetTripStops(int tripId)
        => Db.Map<TripStop>(Db.Query(@"
            SELECT ts.*, s.station_name AS StationName
            FROM trip_stops ts JOIN stations s ON s.station_id = ts.station_id
            WHERE ts.trip_id = @id ORDER BY ts.stop_order", Db.P("@id", tripId)));

    public int GetStopOrder(int tripId, int stationId)
    {
        var o = Db.Scalar("SELECT stop_order FROM trip_stops WHERE trip_id = @t AND station_id = @s",
            Db.P("@t", tripId), Db.P("@s", stationId));
        return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
    }

    /// <summary>Tạo một chuyến tàu + tự sinh lịch ghé ga (theo khoảng cách, tốc độ 55 km/h).</summary>
    public int CreateTrip(int trainId, DateTime date, TimeSpan baseTime)
    {
        Db.Execute("INSERT INTO trips (train_id, departure_date, [status]) VALUES (@t,@d,'scheduled')",
            Db.P("@t", trainId), Db.P("@d", date.Date));
        int tripId = Convert.ToInt32(Db.Scalar("SELECT SCOPE_IDENTITY()"));

        var rs = Db.Query(@"
            SELECT rs.station_id, rs.distance_km
            FROM route_stations rs
            JOIN trains t ON t.route_id = rs.route_id
            WHERE t.train_id = @t ORDER BY rs.stop_order", Db.P("@t", trainId));
        int maxOrder = rs.Rows.Count;

        for (int i = 0; i < maxOrder; i++)
        {
            int stationId = Convert.ToInt32(rs.Rows[i]["station_id"]);
            double km = Convert.ToDouble(rs.Rows[i]["distance_km"]);
            int minutes = (int)Math.Round(km * 60.0 / 55.0);
            DateTime? arr = (i == 0) ? (DateTime?)null : date.Date.Add(baseTime).AddMinutes(minutes);
            DateTime? dep = (i == maxOrder - 1) ? (DateTime?)null
                          : (i == 0 ? date.Date.Add(baseTime) : date.Date.Add(baseTime).AddMinutes(minutes + 5));

            Db.Execute(@"INSERT INTO trip_stops (trip_id, station_id, stop_order, arrival_time, departure_time)
                         VALUES (@t,@s,@o,@a,@d)",
                Db.P("@t", tripId), Db.P("@s", stationId), Db.P("@o", i + 1),
                Db.P("@a", arr), Db.P("@d", dep));
        }
        return tripId;
    }

    public void SetTripStatus(int tripId, string status)
        => Db.Execute("UPDATE trips SET [status] = @s WHERE trip_id = @id", Db.P("@s", status), Db.P("@id", tripId));

    public List<Fare> GetFares()
        => Db.Map<Fare>(Db.Query("SELECT * FROM fares ORDER BY fare_id"));

    public decimal GetFare(int fromId, int toId, int coachTypeId)
    {
        var o = Db.Scalar(@"SELECT base_price FROM fares
            WHERE from_station_id = @f AND to_station_id = @t AND coach_type_id = @c
              AND (effective_to IS NULL OR effective_to >= CAST(SYSDATETIME() AS DATE))",
            Db.P("@f", fromId), Db.P("@t", toId), Db.P("@c", coachTypeId));
        return (o == null || o == DBNull.Value) ? 0 : Convert.ToDecimal(o);
    }

    public decimal GetMinFare(int fromId, int toId)
    {
        var o = Db.Scalar(@"SELECT MIN(base_price) FROM fares
            WHERE from_station_id = @f AND to_station_id = @t
              AND (effective_to IS NULL OR effective_to >= CAST(SYSDATETIME() AS DATE))",
            Db.P("@f", fromId), Db.P("@t", toId));
        return (o == null || o == DBNull.Value) ? 0 : Convert.ToDecimal(o);
    }

    public void SaveFare(Fare fare)
    {
        if (fare.FareId == 0)
            Db.Execute(@"INSERT INTO fares (from_station_id, to_station_id, coach_type_id, base_price, effective_from, effective_to)
                         VALUES (@f,@t,@c,@p,@ef,@et)",
                Db.P("@f", fare.FromStationId), Db.P("@t", fare.ToStationId), Db.P("@c", fare.CoachTypeId),
                Db.P("@p", fare.BasePrice), Db.P("@ef", fare.EffectiveFrom.Date),
                Db.P("@et", (object)fare.EffectiveTo ?? DBNull.Value));
        else
            Db.Execute(@"UPDATE fares SET from_station_id=@f, to_station_id=@t, coach_type_id=@c, base_price=@p,
                         effective_from=@ef, effective_to=@et WHERE fare_id=@id",
                Db.P("@f", fare.FromStationId), Db.P("@t", fare.ToStationId), Db.P("@c", fare.CoachTypeId),
                Db.P("@p", fare.BasePrice), Db.P("@ef", fare.EffectiveFrom.Date),
                Db.P("@et", (object)fare.EffectiveTo ?? DBNull.Value), Db.P("@id", fare.FareId));
    }

    public List<PassengerType> GetPassengerTypes(bool activeOnly = false)
        => Db.Map<PassengerType>(Db.Query("SELECT * FROM passenger_types"
            + (activeOnly ? " WHERE is_active = 1" : "") + " ORDER BY passenger_type_id"));

    public PassengerType GetPassengerType(int id)
        => Db.MapOne<PassengerType>(Db.Query("SELECT * FROM passenger_types WHERE passenger_type_id = @id", Db.P("@id", id)));

    public void SavePassengerType(PassengerType t)
    {
        var idLabel = (object)(string.IsNullOrWhiteSpace(t.IdLabel) ? null : t.IdLabel.Trim());
        if (t.PassengerTypeId == 0)
            Db.Execute(@"INSERT INTO passenger_types (type_name, discount_percent, min_age, max_age, id_label, is_active)
                         VALUES (@n,@d,@min,@max,@l,@a)",
                Db.P("@n", t.TypeName.Trim()), Db.P("@d", t.DiscountPercent),
                Db.P("@min", (object)t.MinAge ?? DBNull.Value), Db.P("@max", (object)t.MaxAge ?? DBNull.Value),
                Db.P("@l", idLabel), Db.P("@a", t.IsActive));
        else
            Db.Execute(@"UPDATE passenger_types SET type_name=@n, discount_percent=@d, min_age=@min,
                         max_age=@max, id_label=@l, is_active=@a WHERE passenger_type_id=@id",
                Db.P("@n", t.TypeName.Trim()), Db.P("@d", t.DiscountPercent),
                Db.P("@min", (object)t.MinAge ?? DBNull.Value), Db.P("@max", (object)t.MaxAge ?? DBNull.Value),
                Db.P("@l", idLabel), Db.P("@a", t.IsActive), Db.P("@id", t.PassengerTypeId));
    }

    public List<TripSearchResultViewModelData> SearchTrips(int fromId, int toId, DateTime date)
    {
        var dt = Db.Query(@"
            SELECT tr.trip_id, tr.train_id, tr.departure_date, t.train_code, t.route_id, r.route_name,
                   dep.departure_time AS DepartureTime, arr.arrival_time AS ArrivalTime,
                   ISNULL(tr.delay_minutes,0) AS DelayMinutes,
                   (SELECT COUNT(*) FROM seats s JOIN coaches c ON c.coach_id = s.coach_id
                      WHERE c.train_id = t.train_id
                        AND NOT EXISTS (
                            SELECT 1 FROM tickets tk
                            JOIN trip_stops tf ON tf.trip_id = tk.trip_id AND tf.station_id = tk.from_station_id
                            JOIN trip_stops tt ON tt.trip_id = tk.trip_id AND tt.station_id = tk.to_station_id
                            JOIN bookings b ON b.booking_id = tk.booking_id
                            WHERE tk.trip_id = tr.trip_id AND tk.seat_id = s.seat_id
                              AND (tk.status = 'paid' OR (tk.status = 'holding' AND b.hold_expires_at > SYSDATETIME()))
                              AND tf.stop_order < arr.stop_order AND tt.stop_order > dep.stop_order
                        )
                   ) AS AvailableSeats
            FROM trips tr
            JOIN trains t ON t.train_id = tr.train_id
            JOIN routes r ON r.route_id = t.route_id
            JOIN trip_stops dep ON dep.trip_id = tr.trip_id AND dep.station_id = @from
            JOIN trip_stops arr ON arr.trip_id = tr.trip_id AND arr.station_id = @to
            WHERE dep.stop_order < arr.stop_order
              AND tr.departure_date = @date
              AND tr.status = 'scheduled' AND t.is_active = 1
            ORDER BY dep.departure_time",
            Db.P("@from", fromId), Db.P("@to", toId), Db.P("@date", date.Date));

        var list = new List<TripSearchResultViewModelData>();
        foreach (DataRow row in dt.Rows)
        {
            list.Add(new TripSearchResultViewModelData
            {
                TripId = Convert.ToInt32(row["trip_id"]),
                TrainId = Convert.ToInt32(row["train_id"]),
                TrainCode = row["train_code"].ToString(),
                RouteId = Convert.ToInt32(row["route_id"]),
                RouteName = row["route_name"].ToString(),
                DepartureDate = Convert.ToDateTime(row["departure_date"]),
                DepartureTime = row["DepartureTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["DepartureTime"]),
                ArrivalTime = row["ArrivalTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["ArrivalTime"]),
                AvailableSeats = Convert.ToInt32(row["AvailableSeats"]),
                DelayMinutes = Convert.ToInt32(row["DelayMinutes"])
            });
        }
        return list;
    }
}
