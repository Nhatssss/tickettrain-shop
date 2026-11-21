using System.Data;
using Microsoft.Data.SqlClient;
using TrainTicketWeb.Models;

using TrainTicketWeb.Data;

namespace TrainTicketWeb.Repositories;

public interface ITrainRepository
{
    List<Train> GetAll();
    Train GetById(int id);
    void Save(Train train);
    List<CoachType> GetCoachTypes();
    void SaveCoachType(CoachType type);
    List<Coach> GetCoaches(int trainId);
    List<Seat> GetSeats(int coachId);
    void ConfigureTrain(int trainId, List<(int coachTypeId, int count)> config);
    int GetCoachTypeOfSeat(int seatId);
}

public class TrainRepository : ITrainRepository
{
    public List<Train> GetAll()
        => Db.Map<Train>(Db.Query(@"
            SELECT t.*, r.route_name AS RouteName
            FROM trains t JOIN routes r ON r.route_id = t.route_id ORDER BY t.train_id"));

    public Train GetById(int id)
        => Db.MapOne<Train>(Db.Query(@"
            SELECT t.*, r.route_name AS RouteName
            FROM trains t JOIN routes r ON r.route_id = t.route_id WHERE t.train_id = @id", Db.P("@id", id)));

    public void Save(Train train)
    {
        if (train.TrainId == 0)
            Db.Execute("INSERT INTO trains (train_code, route_id, is_active) VALUES (@c,@r,@a)",
                Db.P("@c", train.TrainCode.Trim()), Db.P("@r", train.RouteId), Db.P("@a", train.IsActive));
        else
            Db.Execute("UPDATE trains SET train_code=@c, route_id=@r, is_active=@a WHERE train_id=@id",
                Db.P("@c", train.TrainCode.Trim()), Db.P("@r", train.RouteId),
                Db.P("@a", train.IsActive), Db.P("@id", train.TrainId));
    }

    public List<CoachType> GetCoachTypes()
        => Db.Map<CoachType>(Db.Query("SELECT * FROM coach_types ORDER BY coach_type_id"));

    public void SaveCoachType(CoachType type)
    {
        if (type.CoachTypeId == 0)
            Db.Execute("INSERT INTO coach_types (type_name, capacity, description) VALUES (@n,@c,@d)",
                Db.P("@n", type.TypeName.Trim()), Db.P("@c", type.Capacity), Db.P("@d", (type.Description ?? "").Trim()));
        else
            Db.Execute("UPDATE coach_types SET type_name=@n, capacity=@c, description=@d WHERE coach_type_id=@id",
                Db.P("@n", type.TypeName.Trim()), Db.P("@c", type.Capacity),
                Db.P("@d", (type.Description ?? "").Trim()), Db.P("@id", type.CoachTypeId));
    }

    public List<Coach> GetCoaches(int trainId)
        => Db.Map<Coach>(Db.Query(@"
            SELECT c.*, ct.type_name AS TypeName, ct.capacity AS Capacity
            FROM coaches c JOIN coach_types ct ON ct.coach_type_id = c.coach_type_id
            WHERE c.train_id = @id ORDER BY c.coach_number", Db.P("@id", trainId)));

    public List<Seat> GetSeats(int coachId)
        => Db.Map<Seat>(Db.Query("SELECT * FROM seats WHERE coach_id = @id ORDER BY seat_number", Db.P("@id", coachId)));

    public int GetCoachTypeOfSeat(int seatId)
    {
        var dt = Db.Query(@"
            SELECT TOP 1 ct.coach_type_id
            FROM seats s JOIN coaches c ON c.coach_id = s.coach_id
            JOIN coach_types ct ON ct.coach_type_id = c.coach_type_id
            WHERE s.seat_id = @s", Db.P("@s", seatId));
        return dt.Rows.Count == 0 ? 0 : Convert.ToInt32(dt.Rows[0][0]);
    }

    /// <summary>Tạo lại cấu hình toa (số toa, loại toa) + tự sinh ghế cho đoàn tàu.</summary>
    public void ConfigureTrain(int trainId, List<(int coachTypeId, int count)> config)
    {
        Db.InTransaction<object>((c, tx) =>
        {
            var delSeats = new SqlCommand(
                "DELETE FROM seats WHERE coach_id IN (SELECT coach_id FROM coaches WHERE train_id = @t)", c, tx);
            delSeats.Parameters.AddWithValue("@t", trainId);
            delSeats.ExecuteNonQuery();

            var delCoaches = new SqlCommand("DELETE FROM coaches WHERE train_id = @t", c, tx);
            delCoaches.Parameters.AddWithValue("@t", trainId);
            delCoaches.ExecuteNonQuery();

            int coachNo = 1;
            foreach (var (typeId, count) in config)
            {
                for (int k = 0; k < count; k++, coachNo++)
                {
                    var insCoach = new SqlCommand(
                        "INSERT INTO coaches (train_id, coach_number, coach_type_id) OUTPUT INSERTED.coach_id VALUES (@t,@n,@ct)", c, tx);
                    insCoach.Parameters.AddWithValue("@t", trainId);
                    insCoach.Parameters.AddWithValue("@n", coachNo);
                    insCoach.Parameters.AddWithValue("@ct", typeId);
                    int coachId = Convert.ToInt32(insCoach.ExecuteScalar());

                    var capCmd = new SqlCommand("SELECT capacity FROM coach_types WHERE coach_type_id = @ct", c, tx);
                    capCmd.Parameters.AddWithValue("@ct", typeId);
                    int capacity = Convert.ToInt32(capCmd.ExecuteScalar());

                    bool isBerth = (typeId != 1); // 1 = ghế ngồi mềm; còn lại là giường
                    for (int s = 1; s <= capacity; s++)
                    {
                        var insSeat = new SqlCommand(
                            "INSERT INTO seats (coach_id, seat_number, compartment_no, berth_level) VALUES (@c,@sn,@cn,@bl)", c, tx);
                        insSeat.Parameters.AddWithValue("@c", coachId);
                        insSeat.Parameters.AddWithValue("@sn", s.ToString("D2"));
                        insSeat.Parameters.AddWithValue("@cn", isBerth ? (object)((s - 1) / 4 + 1) : DBNull.Value);
                        insSeat.Parameters.AddWithValue("@bl", isBerth ? (object)((s - 1) % 4 + 1) : DBNull.Value);
                        insSeat.ExecuteNonQuery();
                    }
                }
            }
            return null;
        });
    }
}
