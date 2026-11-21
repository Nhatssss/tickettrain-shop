using System.Data;
using TrainTicketWeb.Models;

using TrainTicketWeb.Data;

namespace TrainTicketWeb.Repositories;

public interface IReportRepository
{
    decimal TotalRevenue();
    decimal RevenueBetween(DateTime from, DateTime to);
    int CountBookings();
    int CountPaidTickets();
    int CountCustomers();
    int CountTrips();
    int CountPendingRefunds();
    List<(string label, decimal value)> RevenueByRoute();
    List<(string label, decimal value)> RevenueByDay(int days);
    List<(string label, double percent)> OccupancyByTrip();
}

public class ReportRepository : IReportRepository
{
    private static decimal SafeDec(object o) => (o == null || o == DBNull.Value) ? 0 : Convert.ToDecimal(o);
    private static int SafeInt(object o) => (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);

    public decimal TotalRevenue()
        => SafeDec(Db.Scalar("SELECT ISNULL(SUM(amount),0) FROM payments WHERE [status]='success'"));

    public decimal RevenueBetween(DateTime from, DateTime to)
        => SafeDec(Db.Scalar(
            "SELECT ISNULL(SUM(amount),0) FROM payments WHERE [status]='success' AND paid_at >= @f AND paid_at < @t",
            Db.P("@f", from), Db.P("@t", to)));

    public int CountBookings() => SafeInt(Db.Scalar("SELECT COUNT(*) FROM bookings"));
    public int CountPaidTickets() => SafeInt(Db.Scalar("SELECT COUNT(*) FROM tickets WHERE [status]='paid'"));
    public int CountCustomers() => SafeInt(Db.Scalar("SELECT COUNT(*) FROM users WHERE [role]='customer'"));
    public int CountTrips() => SafeInt(Db.Scalar("SELECT COUNT(*) FROM trips"));
    public int CountPendingRefunds() => SafeInt(Db.Scalar("SELECT COUNT(*) FROM refunds WHERE [status]='requested'"));

    public List<(string label, decimal value)> RevenueByRoute()
    {
        var list = new List<(string, decimal)>();
        var dt = Db.Query(@"
            SELECT r.route_name, ISNULL(SUM(p.amount),0) AS total
            FROM payments p
            JOIN bookings b ON b.booking_id = p.booking_id
            JOIN tickets tk ON tk.booking_id = b.booking_id
            JOIN trips tr ON tr.trip_id = tk.trip_id
            JOIN trains t ON t.train_id = tr.train_id
            JOIN routes r ON r.route_id = t.route_id
            WHERE p.[status]='success'
            GROUP BY r.route_name");
        foreach (DataRow row in dt.Rows)
            list.Add((row["route_name"].ToString(), Convert.ToDecimal(row["total"])));
        return list;
    }

    public List<(string label, decimal value)> RevenueByDay(int days)
    {
        var list = new List<(string, decimal)>();
        var dt = Db.Query(@"
            SELECT CAST(paid_at AS DATE) AS d, ISNULL(SUM(amount),0) AS total
            FROM payments WHERE [status]='success' AND paid_at >= @f
            GROUP BY CAST(paid_at AS DATE) ORDER BY d",
            Db.P("@f", DateTime.Now.AddDays(-days)));
        foreach (DataRow row in dt.Rows)
            list.Add((Convert.ToDateTime(row["d"]).ToString("dd/MM"), Convert.ToDecimal(row["total"])));
        return list;
    }

    public List<(string label, double percent)> OccupancyByTrip()
    {
        var list = new List<(string, double)>();
        var dt = Db.Query(@"
            SELECT TOP 10 tr.trip_id, t.train_code, tr.departure_date,
                   (SELECT COUNT(*) FROM tickets tk WHERE tk.trip_id = tr.trip_id AND tk.status='paid') AS sold,
                   (SELECT COUNT(*) FROM seats s JOIN coaches c ON c.coach_id = s.coach_id WHERE c.train_id = t.train_id) AS total
            FROM trips tr JOIN trains t ON t.train_id = tr.train_id
            ORDER BY tr.departure_date DESC, tr.trip_id");
        foreach (DataRow row in dt.Rows)
        {
            int total = SafeInt(row["total"]);
            int sold = SafeInt(row["sold"]);
            double pct = total == 0 ? 0 : Math.Round(sold * 100.0 / total, 1);
            list.Add(($"{row["train_code"]} {Convert.ToDateTime(row["departure_date"]):dd/MM}", pct));
        }
        return list;
    }
}
