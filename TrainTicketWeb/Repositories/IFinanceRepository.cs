using System.Data;
using Microsoft.Data.SqlClient;
using TrainTicketWeb.Data;
using TrainTicketWeb.Models;
using TrainTicketWeb.ViewModels;

namespace TrainTicketWeb.Repositories;

public interface IFinanceRepository
{
    /// <summary>Tổng tiền hệ thống đang giữ (ví quản trị).</summary>
    FinanceSummary GetSummary();

    /// <summary>Số liệu tiền của một khách (chỉ hiển thị, không phải ví ảo).</summary>
    CustomerFinance GetCustomer(int userId);

    List<CustomerFinance> GetCustomerList();
}

public class FinanceRepository : IFinanceRepository
{
    public FinanceSummary GetSummary()
    {
        var s = new FinanceSummary();

        s.TotalRefunded = Convert.ToDecimal(Db.Scalar(
            "SELECT ISNULL(SUM(refund_amount),0) FROM refunds WHERE [status]='completed'"));

        // Nguồn tiền đã thu coi là "đã thu xong": payments success + payments refunded
        // (khi hoàn 100% do hủy chuyến, payment bị đổi sang 'refunded' nhưng tiền vẫn đã thu thực tế).
        // Nếu không cộng phần 'refunded', quỹ bị âm sai dấu vì refunds vẫn trừ tiếp.
        s.GrossIncome = Convert.ToDecimal(Db.Scalar(
            "SELECT ISNULL(SUM(amount),0) FROM payments WHERE [status] IN ('success','refunded')"));

        s.TotalDiscount = Convert.ToDecimal(Db.Scalar(
            "SELECT ISNULL(SUM(discount_amount),0) FROM bookings WHERE [status]='paid'"));

        s.PaidBookings = Convert.ToInt32(Db.Scalar(
            "SELECT COUNT(*) FROM bookings WHERE [status]='paid'"));

        s.PaidTickets = Convert.ToInt32(Db.Scalar(
            "SELECT COUNT(*) FROM tickets WHERE [status]='paid'"));

        s.PendingRefundAmount = Convert.ToDecimal(Db.Scalar(
            "SELECT ISNULL(SUM(refund_amount),0) FROM refunds WHERE [status]='pending'"));

        s.PendingRefundCount = Convert.ToInt32(Db.Scalar(
            "SELECT COUNT(*) FROM refunds WHERE [status]='pending'"));

        s.CustomerCount = Convert.ToInt32(Db.Scalar(
            "SELECT COUNT(*) FROM users WHERE [role]='customer'"));

        s.LockedCustomerCount = Convert.ToInt32(Db.Scalar(
            "SELECT COUNT(*) FROM users WHERE [role]='customer' AND [status]='locked'"));

        // Tien da thu tru tien da hoan = quy hien tai
        s.Fund = s.GrossIncome - s.TotalRefunded;
        return s;
    }

    public CustomerFinance GetCustomer(int userId)
    {
        var f = new CustomerFinance { UserId = userId };

        var u = Db.MapOne<User>(Db.Query("SELECT * FROM users WHERE user_id=@id", Db.P("@id", userId)));
        if (u == null) return null;
        f.FullName = u.FullName;
        f.Email = u.Email;
        f.Status = u.Status;

        f.TotalSpent = Convert.ToDecimal(Db.Scalar(
            "SELECT ISNULL(SUM(amount),0) FROM payments WHERE [status] IN ('success','refunded') AND booking_id IN " +
            "(SELECT booking_id FROM bookings WHERE user_id=@u)", Db.P("@u", userId)));

        f.TotalRefunded = Convert.ToDecimal(Db.Scalar(
            @"SELECT ISNULL(SUM(r.refund_amount),0) FROM refunds r
              JOIN tickets t ON t.ticket_id = r.ticket_id
              JOIN bookings b ON b.booking_id = t.booking_id
              WHERE b.user_id = @u AND r.[status]='completed'", Db.P("@u", userId)));

        f.TotalDiscountSaved = Convert.ToDecimal(Db.Scalar(
            "SELECT ISNULL(SUM(discount_amount),0) FROM bookings WHERE user_id=@u AND [status]='paid'",
            Db.P("@u", userId)));

        f.BookingCount = Convert.ToInt32(Db.Scalar(
            "SELECT COUNT(*) FROM bookings WHERE user_id=@u AND [status]='paid'", Db.P("@u", userId)));

        f.PendingRefund = Convert.ToDecimal(Db.Scalar(
            @"SELECT ISNULL(SUM(r.refund_amount),0) FROM refunds r
              JOIN tickets t ON t.ticket_id = r.ticket_id
              JOIN bookings b ON b.booking_id = t.booking_id
              WHERE b.user_id = @u AND r.[status]='pending'", Db.P("@u", userId)));

        f.Balance = f.TotalSpent - f.TotalRefunded;
        return f;
    }

    public List<CustomerFinance> GetCustomerList()
    {
        var dt = Db.Query("SELECT user_id FROM users WHERE [role]='customer' ORDER BY created_at DESC");
        var list = new List<CustomerFinance>();
        foreach (DataRow r in dt.Rows)
        {
            var f = GetCustomer(Convert.ToInt32(r["user_id"]));
            if (f != null) list.Add(f);
        }
        return list;
    }
}
