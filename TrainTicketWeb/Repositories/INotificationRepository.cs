using TrainTicketWeb.Data;
using TrainTicketWeb.Models;

namespace TrainTicketWeb.Repositories;

public interface INotificationRepository
{
    List<Notification> GetForUser(int userId, int take = 30);
    int CountUnread(int userId);
    Notification GetById(int id, int userId);
    int Add(Notification n);
    void MarkRead(int id, int userId);
    void MarkAllRead(int userId);

    /// <summary>Them thong bao, bo qua neu da co cung (user, type, trip). Tra ve id, 0 neu trung.</summary>
    int AddOnce(Notification n);

    /// <summary>Khach da mua ve cua chuyen nay (de gui thong bao hoan/huy).</summary>
    List<int> GetTripCustomerIds(int tripId);

    List<int> GetTripTicketHolders(int tripId);
}

public class NotificationRepository : INotificationRepository
{
    private const string Cols = @"notification_id, user_id, type, title, body, link_url,
        ref_trip_id, ref_coupon_code, is_read, created_at";

    public List<Notification> GetForUser(int userId, int take = 30)
        => Db.Map<Notification>(Db.Query(
            $@"SELECT TOP ({take}) {Cols} FROM notifications
               WHERE user_id = @u ORDER BY created_at DESC", Db.P("@u", userId)));

    public int CountUnread(int userId)
        => Convert.ToInt32(Db.Scalar(
            "SELECT COUNT(*) FROM notifications WHERE user_id=@u AND is_read=0", Db.P("@u", userId)));

    public Notification GetById(int id, int userId)
        => Db.MapOne<Notification>(Db.Query(
            $"SELECT {Cols} FROM notifications WHERE notification_id=@id AND user_id=@u",
            Db.P("@id", id), Db.P("@u", userId)));

    public int Add(Notification n)
    {
        // OUTPUT INSERTED để lấy id trên cùng connection (SCOPE_IDENTITY qua connection khác sẽ trả NULL)
        using var conn = Db.Open();
        using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"INSERT INTO notifications (user_id, type, title, body, link_url, ref_trip_id, ref_coupon_code, is_read, created_at)
                     OUTPUT INSERTED.notification_id
                     VALUES (@u,@t,@ti,@b,@l,@rt,@rc,0,SYSDATETIME())", conn);
        cmd.Parameters.AddWithValue("@u", n.UserId);
        cmd.Parameters.AddWithValue("@t", n.Type ?? "system");
        cmd.Parameters.AddWithValue("@ti", n.Title ?? "");
        cmd.Parameters.AddWithValue("@b", (object)n.Body ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@l", (object)n.LinkUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@rt", (object)n.RefTripId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@rc", (object)n.RefCouponCode ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int AddOnce(Notification n)
    {
        if (n.RefTripId.HasValue)
        {
            int dup = Convert.ToInt32(Db.Scalar(
                @"SELECT COUNT(*) FROM notifications
                  WHERE user_id=@u AND type=@t AND ref_trip_id=@rt",
                Db.P("@u", n.UserId), Db.P("@t", n.Type), Db.P("@rt", n.RefTripId.Value)));
            if (dup > 0) return 0;
        }
        try
        {
            return Add(n);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            // Trung unique index (user, type, ref_trip_id) -> da co thong bao nay
            return 0;
        }
    }

    public void MarkRead(int id, int userId)
        => Db.Execute("UPDATE notifications SET is_read=1 WHERE notification_id=@id AND user_id=@u",
            Db.P("@id", id), Db.P("@u", userId));

    public void MarkAllRead(int userId)
        => Db.Execute("UPDATE notifications SET is_read=1 WHERE user_id=@u AND is_read=0",
            Db.P("@u", userId));

    public List<int> GetTripCustomerIds(int tripId)
    {
        var dt = Db.Query(@"SELECT DISTINCT b.user_id
                            FROM tickets tk
                            JOIN bookings b ON b.booking_id = tk.booking_id
                            WHERE tk.trip_id = @t AND tk.[status] IN ('paid','cancelled')",
            Db.P("@t", tripId));
        var list = new List<int>();
        foreach (System.Data.DataRow r in dt.Rows) list.Add(Convert.ToInt32(r["user_id"]));
        return list;
    }

    public List<int> GetTripTicketHolders(int tripId)
    {
        var dt = Db.Query(@"SELECT DISTINCT b.user_id
                            FROM tickets tk
                            JOIN bookings b ON b.booking_id = tk.booking_id
                            WHERE tk.trip_id = @t AND tk.[status] = 'paid'",
            Db.P("@t", tripId));
        var list = new List<int>();
        foreach (System.Data.DataRow r in dt.Rows) list.Add(Convert.ToInt32(r["user_id"]));
        return list;
    }
}
