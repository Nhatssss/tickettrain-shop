using TrainTicketWeb.Data;
using TrainTicketWeb.Hubs;
using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;

namespace TrainTicketWeb.Services;

public interface ITripAdminService
{
    /// <summary>Hoàn chuyến: đặt delay_minutes, gửi thông báo real-time cho khách đã mua vé.</summary>
    (bool ok, string msg, int notified) DelayTrip(int tripId, int minutes, string note);

    /// <summary>Hủy chuyến: hoàn tiền 100% tự động + thông báo + tặng mã GIFF555.</summary>
    (bool ok, string msg, int refunded, decimal amount, int notified) CancelTrip(int tripId, string reason, int adminId);

    /// <summary>Xóa chuyến: chỉ xóa cứng được khi chưa bán vé nào.</summary>
    (bool ok, string msg) DeleteTrip(int tripId);

    /// <summary>Bỏ hoãn (đưa delay về 0).</summary>
    (bool ok, string msg) ClearDelay(int tripId);

    /// <summary>Chi tiết chuyến: danh sách vé đã bán, doanh thu, thông báo đã gửi.</summary>
    TripDetailHoanViewModel GetDetail(int tripId);
}

/// <summary>Một vé trong chi tiết chuyến.</summary>
public class TripDetailTicketRow
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; }
    public string PassengerName { get; set; }
    public string SeatNumber { get; set; }
    public int CoachNumber { get; set; }
    public string CoachTypeName { get; set; }
    public decimal FinalPrice { get; set; }
    public string Status { get; set; }
    public string CustomerEmail { get; set; }
    public string BookingCode { get; set; }
    public string PassengerTypeName { get; set; }
}

public class TripDetailHoanViewModel
{
    public Trip Trip { get; set; }
    public List<TripDetailTicketRow> Tickets { get; set; } = new();
    public List<TripStop> Stops { get; set; } = new();
    public int SoldTickets { get; set; }
    public decimal Revenue { get; set; }
    public decimal Refunded { get; set; }
    public int CustomerCount { get; set; }
    public int NotifiedCount { get; set; }
}

public class TripAdminService : ITripAdminService
{
    private const string CouponGift = "GIFF555";

    private readonly IScheduleRepository _sched;
    private readonly INotificationRepository _notes;
    private readonly INotificationPusher _push;
    private readonly IBookingRepository _bookings;

    public TripAdminService(IScheduleRepository sched, INotificationRepository notes,
        INotificationPusher push, IBookingRepository bookings)
    {
        _sched = sched;
        _notes = notes;
        _push = push;
        _bookings = bookings;
    }

    public (bool ok, string msg, int notified) DelayTrip(int tripId, int minutes, string note)
    {
        var trip = _sched.GetTrip(tripId);
        if (trip == null) return (false, "Không tìm thấy chuyến.", 0);
        if (minutes <= 0) return (false, "Số phút hoãn phải lớn hơn 0.", 0);
        if (trip.IsCancelled) return (false, "Chuyến đã hủy, không thể hoãn.", 0);

        Db.Execute("UPDATE trips SET delay_minutes=@m WHERE trip_id=@id",
            Db.P("@m", minutes), Db.P("@id", tripId));

        // Day gio ghe ga theo so phut hoan
        Db.Execute(@"UPDATE trip_stops
                     SET arrival_time   = DATEADD(MINUTE, @m, arrival_time),
                         departure_time = DATEADD(MINUTE, @m, departure_time)
                     WHERE trip_id = @id",
            Db.P("@m", minutes), Db.P("@id", tripId));

        int notified = 0;
        foreach (var uid in _notes.GetTripCustomerIds(tripId))
        {
            var n = new Notification
            {
                UserId = uid,
                Type = "trip_delayed",
                Title = $"Chuyến {trip.TrainCode} ngày {trip.DepartureDate:dd/MM/yyyy} bị hoãn {minutes} phút",
                Body = string.IsNullOrWhiteSpace(note)
                    ? $"Tàu {trip.TrainCode} ({trip.RouteName}) khởi hành muộn {minutes} phút so với giờ dự kiến. Giờ mới đã được cập nhật trên vé của bạn."
                    : note,
                LinkUrl = "/Booking/MyBookings",
                RefTripId = tripId
            };
            int id = _notes.AddOnce(n);
            if (id > 0)
            {
                notified++;
                n.NotificationId = id;
                _push.PushAsync(uid, n, _notes.CountUnread(uid));
            }
        }
        return (true, $"Đã hoãn chuyến {minutes} phút.", notified);
    }

    public (bool ok, string msg) ClearDelay(int tripId)
    {
        var trip = _sched.GetTrip(tripId);
        if (trip == null) return (false, "Không tìm thấy chuyến.");
        if (trip.DelayMinutes <= 0) return (false, "Chuyến này không bị hoãn.");

        int m = trip.DelayMinutes;
        Db.Execute("UPDATE trips SET delay_minutes=0 WHERE trip_id=@id", Db.P("@id", tripId));
        Db.Execute(@"UPDATE trip_stops
                     SET arrival_time   = DATEADD(MINUTE, -@m, arrival_time),
                         departure_time = DATEADD(MINUTE, -@m, departure_time)
                     WHERE trip_id = @id", Db.P("@m", m), Db.P("@id", tripId));
        return (true, "Đã bỏ trạng thái hoãn, giờ tàu trở lại bình thường.");
    }

    public (bool ok, string msg, int refunded, decimal amount, int notified) CancelTrip(int tripId, string reason, int adminId)
    {
        var trip = _sched.GetTrip(tripId);
        if (trip == null) return (false, "Không tìm thấy chuyến.", 0, 0, 0);
        if (trip.IsCancelled) return (false, "Chuyến này đã bị hủy trước đó.", 0, 0, 0);

        if (string.IsNullOrWhiteSpace(reason))
            reason = "Chuyến tàu bị hủy bởi nhà vận hành.";

        int refunded = 0;
        decimal totalAmount = 0;
        var holders = _notes.GetTripTicketHolders(tripId);

        Db.InTransaction<object>((c, tx) =>
        {
            // 1. Danh dau chuyen huy
            var up = new Microsoft.Data.SqlClient.SqlCommand(
                "UPDATE trips SET [status]='cancelled', cancel_reason=@r WHERE trip_id=@id", c, tx);
            up.Parameters.AddWithValue("@r", reason);
            up.Parameters.AddWithValue("@id", tripId);
            up.ExecuteNonQuery();

            // 2. Tat ca ve paid cua chuyen -> hoan 100% tu dong
            var q = new Microsoft.Data.SqlClient.SqlCommand(@"
                SELECT tk.ticket_id, tk.final_price, tk.booking_id
                FROM tickets tk
                WHERE tk.trip_id = @id AND tk.[status] = 'paid'", c, tx);
            q.Parameters.AddWithValue("@id", tripId);

            var rows = new List<(int ticketId, decimal price, int bookingId)>();
            using (var rd = q.ExecuteReader())
            {
                while (rd.Read())
                    rows.Add((Convert.ToInt32(rd["ticket_id"]),
                              Convert.ToDecimal(rd["final_price"]),
                              Convert.ToInt32(rd["booking_id"])));
            }

            foreach (var row in rows)
            {
                // Moi ve: ghi refund completed, phi = 0, so tien = dung gia ve (100%)
                var ins = new Microsoft.Data.SqlClient.SqlCommand(@"
                    INSERT INTO refunds (ticket_id, payment_id, reason, refund_fee, refund_amount, [status], requested_at, processed_by, processed_at)
                    VALUES (@t, (SELECT TOP 1 p.payment_id FROM payments p WHERE p.booking_id=@b AND p.[status]='success' ORDER BY p.payment_id),
                            @r, 0, @amt, 'completed', SYSDATETIME(), @a, SYSDATETIME())", c, tx);
                ins.Parameters.AddWithValue("@t", row.ticketId);
                ins.Parameters.AddWithValue("@b", row.bookingId);
                ins.Parameters.AddWithValue("@r", "Hủy chuyến — hoàn 100%: " + reason);
                ins.Parameters.AddWithValue("@amt", row.price);
                ins.Parameters.AddWithValue("@a", adminId);
                ins.ExecuteNonQuery();

                var upT = new Microsoft.Data.SqlClient.SqlCommand(
                    "UPDATE tickets SET [status]='cancelled' WHERE ticket_id=@t", c, tx);
                upT.Parameters.AddWithValue("@t", row.ticketId);
                upT.ExecuteNonQuery();

                refunded++;
                totalAmount += row.price;
            }

            // 3. Booking nao toan bo ve da huy -> danh dau cancelled, khong con treo
            var qb = new Microsoft.Data.SqlClient.SqlCommand(@"
                UPDATE b SET b.[status]='cancelled'
                FROM bookings b
                WHERE b.[status]='paid'
                  AND NOT EXISTS (SELECT 1 FROM tickets tk WHERE tk.booking_id=b.booking_id AND tk.[status]='paid')
                  AND EXISTS (SELECT 1 FROM tickets tk2 WHERE tk2.booking_id=b.booking_id AND tk2.trip_id=@id)", c, tx);
            qb.Parameters.AddWithValue("@id", tripId);
            qb.ExecuteNonQuery();

            // 4. Payment chi chuyen 'refunded' khi moi ve cua don deu khong con paid
            var qp = new Microsoft.Data.SqlClient.SqlCommand(@"
                UPDATE p SET p.[status]='refunded'
                FROM payments p
                WHERE p.[status]='success'
                  AND NOT EXISTS (SELECT 1 FROM tickets tk WHERE tk.booking_id=p.booking_id AND tk.[status]='paid')
                  AND EXISTS (SELECT 1 FROM tickets tk2 WHERE tk2.booking_id=p.booking_id AND tk2.trip_id=@id)", c, tx);
            qp.Parameters.AddWithValue("@id", tripId);
            qp.ExecuteNonQuery();

            return null;
        });

        // 5. Thong bao real-time + tang ma GIFF555
        int notified = 0;
        var coupon = Db.MapOne<Coupon>(Db.Query(
            "SELECT * FROM coupons WHERE code=@c", Db.P("@c", CouponGift)));

        foreach (var uid in holders)
        {
            var n = new Notification
            {
                UserId = uid,
                Type = "trip_cancelled",
                Title = $"Chuyến {trip.TrainCode} ngày {trip.DepartureDate:dd/MM/yyyy} đã bị hủy",
                Body = $"Chúng tôi đã hoàn 100% tiền vé vào tài khoản của bạn. {reason}"
                       + (coupon != null ? $" Tặng bạn mã {CouponGift} giảm {coupon.DiscountLabel} cho lần đặt vé tới." : ""),
                LinkUrl = "/Booking/MyBookings",
                RefTripId = tripId
            };
            int id = _notes.AddOnce(n);
            if (id > 0)
            {
                notified++;
                n.NotificationId = id;
                _push.PushAsync(uid, n, _notes.CountUnread(uid));
            }

            if (coupon != null)
            {
                var nc = new Notification
                {
                    UserId = uid,
                    Type = "coupon",
                    Title = $"Tặng mã {CouponGift} — giảm {coupon.DiscountLabel}",
                    Body = "Lời xin lỗi vì chuyến tàu bị hủy. Áp mã này ở bước thanh toán cho lần đặt vé tiếp theo.",
                    LinkUrl = "/",
                    RefCouponCode = CouponGift
                };
                if (_notes.AddOnce(nc) > 0)
                    _push.PushAsync(uid, nc, _notes.CountUnread(uid));
            }
        }

        return (true, $"Đã hủy chuyến và hoàn {totalAmount:N0} ₫ cho {refunded} vé.", refunded, totalAmount, notified);
    }

    public (bool ok, string msg) DeleteTrip(int tripId)
    {
        var trip = _sched.GetTrip(tripId);
        if (trip == null) return (false, "Không tìm thấy chuyến.");

        // Kiểm tra mọi vé (không chỉ 'paid'): vé 'cancelled' vẫn tham chiếu trips -> sẽ vướng FK_tickets_trip.
        int anyTickets = Convert.ToInt32(Db.Scalar(
            "SELECT COUNT(*) FROM tickets WHERE trip_id=@id", Db.P("@id", tripId)));
        if (anyTickets > 0)
            return (false, $"Chuyến đã có {anyTickets} vé (kể cả vé đã hủy) nên không thể xóa. Hãy dùng chức năng Hủy chuyến.");

        // Không có vé -> dọn các bảng con còn tham chiếu tới chuyến (FK) rồi mới xóa trips.
        Db.InTransaction<object>((c, tx) =>
        {
            foreach (var sql in new[]
            {
                "DELETE FROM trip_stops WHERE trip_id=@id",
                "DELETE FROM refunds WHERE trip_id=@id",   // nếu bảng refunds có cột trip_id
            })
            {
                try
                {
                    var d = new Microsoft.Data.SqlClient.SqlCommand(sql, c, tx);
                    d.Parameters.AddWithValue("@id", tripId);
                    d.ExecuteNonQuery();
                }
                catch (Microsoft.Data.SqlClient.SqlException) { /* cột/bảng không tồn tại -> bỏ qua */ }
            }

            var d2 = new Microsoft.Data.SqlClient.SqlCommand("DELETE FROM trips WHERE trip_id=@id", c, tx);
            d2.Parameters.AddWithValue("@id", tripId);
            d2.ExecuteNonQuery();
            return null;
        });
        return (true, "Đã xóa chuyến tàu.");
    }

    public TripDetailHoanViewModel GetDetail(int tripId)
    {
        var trip = _sched.GetTrip(tripId);
        if (trip == null) return null;

        var vm = new TripDetailHoanViewModel
        {
            Trip = trip,
            Stops = _sched.GetTripStops(tripId)
        };

        var dt = Db.Query(@"
            SELECT tk.ticket_id, tk.ticket_code, tk.passenger_name, tk.final_price, tk.[status],
                   s.seat_number, c.coach_number, ct.type_name AS CoachTypeName,
                   pt.type_name AS PassengerTypeName,
                   u.email AS CustomerEmail, b.booking_code AS BookingCode
            FROM tickets tk
            JOIN seats s ON s.seat_id = tk.seat_id
            JOIN coaches c ON c.coach_id = s.coach_id
            JOIN coach_types ct ON ct.coach_type_id = c.coach_type_id
            LEFT JOIN passenger_types pt ON pt.passenger_type_id = tk.passenger_type_id
            JOIN bookings b ON b.booking_id = tk.booking_id
            JOIN users u ON u.user_id = b.user_id
            WHERE tk.trip_id = @id
            ORDER BY c.coach_number, s.seat_number", Db.P("@id", tripId));

        foreach (System.Data.DataRow r in dt.Rows)
        {
            vm.Tickets.Add(new TripDetailTicketRow
            {
                TicketId = Convert.ToInt32(r["ticket_id"]),
                TicketCode = r["ticket_code"].ToString(),
                PassengerName = r["passenger_name"] == DBNull.Value ? "" : r["passenger_name"].ToString(),
                SeatNumber = r["seat_number"].ToString(),
                CoachNumber = Convert.ToInt32(r["coach_number"]),
                CoachTypeName = r["CoachTypeName"] == DBNull.Value ? "" : r["CoachTypeName"].ToString(),
                FinalPrice = Convert.ToDecimal(r["final_price"]),
                Status = r["status"].ToString(),
                CustomerEmail = r["CustomerEmail"] == DBNull.Value ? "" : r["CustomerEmail"].ToString(),
                BookingCode = r["BookingCode"].ToString(),
                PassengerTypeName = r["PassengerTypeName"] == DBNull.Value ? "" : r["PassengerTypeName"].ToString()
            });
        }

        vm.SoldTickets = vm.Tickets.Count(t => t.Status == "paid");
        vm.Revenue = vm.Tickets.Where(t => t.Status == "paid").Sum(t => t.FinalPrice);
        vm.CustomerCount = vm.Tickets.Where(t => t.Status == "paid")
            .Select(t => t.CustomerEmail).Distinct().Count();

        vm.Refunded = Convert.ToDecimal(Db.Scalar(@"
            SELECT ISNULL(SUM(rf.refund_amount),0)
            FROM refunds rf
            JOIN tickets tk ON tk.ticket_id = rf.ticket_id
            WHERE tk.trip_id = @id AND rf.[status] = 'completed'", Db.P("@id", tripId)));

        vm.NotifiedCount = Convert.ToInt32(Db.Scalar(
            "SELECT COUNT(*) FROM notifications WHERE ref_trip_id = @id", Db.P("@id", tripId)));

        return vm;
    }
}
