using System.Data;
using Microsoft.Data.SqlClient;
using TrainTicketWeb.Models;
using TrainTicketWeb.ViewModels;

using TrainTicketWeb.Data;

namespace TrainTicketWeb.Repositories;

public interface IBookingRepository
{
    void ExpireHeld();
    List<SeatMapCoachViewModel> GetSeatMap(int tripId, int fromId, int toId);
    (int bookingId, string error) Create(int userId, string tripType, string email, string phone, List<SeatPick> picks);
    Booking GetById(int id);
    List<PassengerRow> GetPassengers(int bookingId);
    void UpdatePassengers(int bookingId, List<PassengerRow> rows);
    bool Pay(int bookingId, string method);
    List<Booking> GetByUser(int userId);
    List<Payment> GetPayments(int bookingId);
    TicketDetailViewModel GetTicket(int ticketId);
    BookingDetailViewModel GetBookingDetail(int bookingId);
    (bool ok, string msg, decimal fee, decimal refund) RequestRefund(int ticketId, int userId);
    List<Booking> GetAll(string status = null);
    List<RefundDetailViewModel> GetRefunds();
    void ProcessRefund(int refundId, int adminId, bool approve);
}

public class BookingRepository : IBookingRepository
{
    private static readonly Random Rng = new Random();

    // ===== Giữ chỗ hết hạn =====
    public void ExpireHeld()
    {
        Db.Execute(@"UPDATE tickets SET [status] = 'expired'
                     WHERE [status] = 'holding'
                       AND booking_id IN (SELECT booking_id FROM bookings WHERE [status] = 'holding' AND hold_expires_at <= SYSDATETIME())");
        Db.Execute(@"UPDATE bookings SET [status] = 'expired'
                     WHERE [status] = 'holding' AND hold_expires_at <= SYSDATETIME()");
    }

    // ===== Sơ đồ toa =====
    public List<SeatMapCoachViewModel> GetSeatMap(int tripId, int fromId, int toId)
    {
        var sched = new ScheduleRepository();
        ExpireHeld();
        int fromOrder = sched.GetStopOrder(tripId, fromId);
        int toOrder = sched.GetStopOrder(tripId, toId);

        var dt = Db.Query(@"
            SELECT s.seat_id, s.coach_id, s.seat_number, s.compartment_no, s.berth_level,
                   c.coach_number, c.coach_type_id, ct.type_name,
                   CASE
                     WHEN EXISTS (
                        SELECT 1 FROM tickets tk
                        JOIN trip_stops tf ON tf.trip_id = tk.trip_id AND tf.station_id = tk.from_station_id
                        JOIN trip_stops tt ON tt.trip_id = tk.trip_id AND tt.station_id = tk.to_station_id
                        WHERE tk.trip_id = @trip AND tk.seat_id = s.seat_id AND tk.status = 'paid'
                          AND tf.stop_order < @toOrder AND tt.stop_order > @fromOrder
                     ) THEN 'sold'
                     WHEN EXISTS (
                        SELECT 1 FROM tickets tk
                        JOIN trip_stops tf ON tf.trip_id = tk.trip_id AND tf.station_id = tk.from_station_id
                        JOIN trip_stops tt ON tt.trip_id = tk.trip_id AND tt.station_id = tk.to_station_id
                        JOIN bookings b ON b.booking_id = tk.booking_id
                        WHERE tk.trip_id = @trip AND tk.seat_id = s.seat_id AND tk.status = 'holding'
                          AND b.hold_expires_at > SYSDATETIME()
                          AND tf.stop_order < @toOrder AND tt.stop_order > @fromOrder
                     ) THEN 'held'
                     ELSE 'free'
                   END AS SeatStatus
            FROM seats s
            JOIN coaches c ON c.coach_id = s.coach_id
            JOIN coach_types ct ON ct.coach_type_id = c.coach_type_id
            WHERE c.train_id = (SELECT train_id FROM trips WHERE trip_id = @trip)
            ORDER BY c.coach_number, s.seat_number",
            Db.P("@trip", tripId), Db.P("@fromOrder", fromOrder), Db.P("@toOrder", toOrder));

        var coaches = new List<SeatMapCoachViewModel>();
        foreach (DataRow row in dt.Rows)
        {
            int coachId = Convert.ToInt32(row["coach_id"]);
            var coach = coaches.FirstOrDefault(x => x.CoachId == coachId);
            if (coach == null)
            {
                coach = new SeatMapCoachViewModel
                {
                    CoachId = coachId,
                    CoachNumber = Convert.ToInt32(row["coach_number"]),
                    CoachTypeId = Convert.ToInt32(row["coach_type_id"]),
                    TypeName = row["type_name"].ToString()
                };
                coaches.Add(coach);
            }
            coach.Seats.Add(new SeatMapSeatViewModel
            {
                SeatId = Convert.ToInt32(row["seat_id"]),
                SeatNumber = row["seat_number"].ToString(),
                CompartmentNo = row["compartment_no"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["compartment_no"]),
                BerthLevel = row["berth_level"] == DBNull.Value ? (byte?)null : Convert.ToByte(row["berth_level"]),
                Status = row["SeatStatus"].ToString()
            });
        }
        return coaches;
    }

    // ===== Tạo đơn giữ chỗ =====
    public (int bookingId, string error) Create(int userId, string tripType, string email, string phone, List<SeatPick> picks)
    {
        var sched = new ScheduleRepository();
        return Db.InTransaction<(int, string)>((c, tx) =>
        {
            ExpireHeldInTx(c, tx);

            string code = NewBookingCode(c, tx);
            var ins = new SqlCommand(@"
                INSERT INTO bookings (booking_code, user_id, trip_type, contact_email, contact_phone, total_amount, discount_amount, [status], hold_expires_at, created_at)
                VALUES (@c,@u,@t,@e,@p,0,0,'holding',DATEADD(MINUTE,@hold,SYSDATETIME()),SYSDATETIME());
                SELECT SCOPE_IDENTITY();", c, tx);
            ins.Parameters.AddWithValue("@c", code);
            ins.Parameters.AddWithValue("@u", userId);
            ins.Parameters.AddWithValue("@t", tripType);
            ins.Parameters.AddWithValue("@e", email ?? "");
            ins.Parameters.AddWithValue("@p", phone ?? "");
            ins.Parameters.AddWithValue("@hold", Services.AppSettings.HoldMinutes);
            int bookingId = Convert.ToInt32(ins.ExecuteScalar());

            foreach (var pk in picks)
            {
                int fromOrder = GetStopOrderInTx(c, tx, pk.TripId, pk.FromId);
                int toOrder = GetStopOrderInTx(c, tx, pk.TripId, pk.ToId);

                var chk = new SqlCommand(@"
                    SELECT COUNT(*) FROM tickets tk
                    JOIN trip_stops tf ON tf.trip_id = tk.trip_id AND tf.station_id = tk.from_station_id
                    JOIN trip_stops tt ON tt.trip_id = tk.trip_id AND tt.station_id = tk.to_station_id
                    JOIN bookings b ON b.booking_id = tk.booking_id
                    WHERE tk.trip_id = @trip AND tk.seat_id = @seat
                      AND (tk.status = 'paid' OR (tk.status = 'holding' AND b.hold_expires_at > SYSDATETIME()))
                      AND tf.stop_order < @toOrder AND tt.stop_order > @fromOrder", c, tx);
                chk.Parameters.AddWithValue("@trip", pk.TripId);
                chk.Parameters.AddWithValue("@seat", pk.SeatId);
                chk.Parameters.AddWithValue("@toOrder", toOrder);
                chk.Parameters.AddWithValue("@fromOrder", fromOrder);
                if (Convert.ToInt32(chk.ExecuteScalar()) > 0)
                {
                    tx.Rollback();
                    return (0, $"Chỗ {GetSeatNumberInTx(c, tx, pk.SeatId)} vừa được đặt bởi người khác. Vui lòng chọn lại.");
                }

                decimal basePrice = GetFareInTx(c, tx, pk.FromId, pk.ToId, pk.CoachTypeId);
                string tcode = NewTicketCode(c, tx);
                var insT = new SqlCommand(@"
                    INSERT INTO tickets (ticket_code, booking_id, trip_id, seat_id, from_station_id, to_station_id, leg,
                                         passenger_type_id, passenger_name, id_number, original_price, discount_amount, final_price, [status], created_at)
                    VALUES (@c,@b,@trip,@seat,@f,@to,@leg,
                            (SELECT TOP 1 passenger_type_id FROM passenger_types WHERE type_name = N'Người lớn'),
                            N'', '', @price, 0, @price, 'holding', SYSDATETIME())", c, tx);
                insT.Parameters.AddWithValue("@c", tcode);
                insT.Parameters.AddWithValue("@b", bookingId);
                insT.Parameters.AddWithValue("@trip", pk.TripId);
                insT.Parameters.AddWithValue("@seat", pk.SeatId);
                insT.Parameters.AddWithValue("@f", pk.FromId);
                insT.Parameters.AddWithValue("@to", pk.ToId);
                insT.Parameters.AddWithValue("@leg", pk.Leg ?? "outbound");
                insT.Parameters.AddWithValue("@price", basePrice);
                insT.ExecuteNonQuery();
            }
            return (bookingId, (string)null);
        });
    }

    private static decimal GetFareInTx(SqlConnection c, SqlTransaction tx, int fromId, int toId, int coachTypeId)
    {
        var cmd = new SqlCommand(@"SELECT base_price FROM fares
            WHERE from_station_id = @f AND to_station_id = @t AND coach_type_id = @c
              AND (effective_to IS NULL OR effective_to >= CAST(SYSDATETIME() AS DATE))", c, tx);
        cmd.Parameters.AddWithValue("@f", fromId);
        cmd.Parameters.AddWithValue("@t", toId);
        cmd.Parameters.AddWithValue("@c", coachTypeId);
        var o = cmd.ExecuteScalar();
        return (o == null || o == DBNull.Value) ? 0 : Convert.ToDecimal(o);
    }
    private static void ExpireHeldInTx(SqlConnection c, SqlTransaction tx)
    {
        var e1 = new SqlCommand(@"UPDATE tickets SET [status]='expired'
            WHERE [status]='holding' AND booking_id IN (SELECT booking_id FROM bookings WHERE [status]='holding' AND hold_expires_at <= SYSDATETIME())", c, tx);
        e1.ExecuteNonQuery();
        var e2 = new SqlCommand(@"UPDATE bookings SET [status]='expired'
            WHERE [status]='holding' AND hold_expires_at <= SYSDATETIME()", c, tx);
        e2.ExecuteNonQuery();
    }

    private static int GetStopOrderInTx(SqlConnection c, SqlTransaction tx, int tripId, int stationId)
    {
        var cmd = new SqlCommand("SELECT stop_order FROM trip_stops WHERE trip_id=@t AND station_id=@s", c, tx);
        cmd.Parameters.AddWithValue("@t", tripId);
        cmd.Parameters.AddWithValue("@s", stationId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static string GetSeatNumberInTx(SqlConnection c, SqlTransaction tx, int seatId)
    {
        var cmd = new SqlCommand("SELECT seat_number FROM seats WHERE seat_id=@s", c, tx);
        cmd.Parameters.AddWithValue("@s", seatId);
        var o = cmd.ExecuteScalar();
        return o == null || o == DBNull.Value ? seatId.ToString() : o.ToString();
    }

    // booking_code VARCHAR(12): "BK" + yyMMdd (6) + 4 số = 12 ký tự
    private static string NewBookingCode(SqlConnection c, SqlTransaction tx = null)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var code = "BK" + DateTime.Now.ToString("yyMMdd") + Rng.Next(1000, 10000).ToString("D4");
            if (!CodeExists(c, "bookings", "booking_code", code, tx)) return code;
        }
        return "BK" + DateTime.Now.ToString("yyMMdd") + Rng.Next(1000, 10000).ToString("D4");
    }

    // ticket_code VARCHAR(15): "TK" + yyMMdd (6) + 6 số = 14 ký tự
    private static string NewTicketCode(SqlConnection c, SqlTransaction tx = null)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var code = "TK" + DateTime.Now.ToString("yyMMdd") + Rng.Next(100000, 1000000).ToString("D6");
            if (!CodeExists(c, "tickets", "ticket_code", code, tx)) return code;
        }
        return "TK" + DateTime.Now.ToString("yyMMdd") + Rng.Next(100000, 1000000).ToString("D6");
    }

    private static bool CodeExists(SqlConnection c, string table, string column, string code, SqlTransaction tx = null)
    {
        var cmd = Db.WithTx(new SqlCommand($"SELECT COUNT(*) FROM {table} WHERE {column} = @code", c));
        if (tx != null) cmd.Transaction = tx;
        cmd.Parameters.AddWithValue("@code", code);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
    // ===== Đơn / vé =====
    public Booking GetById(int id)
        => Db.MapOne<Booking>(Db.Query("SELECT * FROM bookings WHERE booking_id = @id", Db.P("@id", id)));

    public List<PassengerRow> GetPassengers(int bookingId)
    {
        var rows = new List<PassengerRow>();
        var dt = Db.Query(@"
            SELECT tk.ticket_id, tk.ticket_code, tk.leg, c.coach_type_id, tk.passenger_type_id,
                   tk.passenger_name, tk.id_number, tk.original_price,
                   tn.train_code, tr.departure_date,
                   s.seat_number, c.coach_number,
                   dep.station_name AS FromName, arr.station_name AS ToName
            FROM tickets tk
            JOIN trips tr ON tr.trip_id = tk.trip_id
            JOIN trains tn ON tn.train_id = tr.train_id
            JOIN seats s ON s.seat_id = tk.seat_id
            JOIN coaches c ON c.coach_id = s.coach_id
            JOIN stations dep ON dep.station_id = tk.from_station_id
            JOIN stations arr ON arr.station_id = tk.to_station_id
            WHERE tk.booking_id = @id
            ORDER BY tk.leg, tk.ticket_id", Db.P("@id", bookingId));

        foreach (DataRow r in dt.Rows)
        {
            rows.Add(new PassengerRow
            {
                TicketId = Convert.ToInt32(r["ticket_id"]),
                TicketCode = r["ticket_code"].ToString(),
                Leg = r["leg"].ToString(),
                CoachTypeId = Convert.ToInt32(r["coach_type_id"]),
                PassengerTypeId = Convert.ToInt32(r["passenger_type_id"]),
                PassengerName = r["passenger_name"].ToString(),
                IdNumber = r["id_number"].ToString(),
                OriginalPrice = Convert.ToDecimal(r["original_price"]),
                TripLabel = $"{r["train_code"]} · {Convert.ToDateTime(r["departure_date"]):dd/MM/yyyy} · {r["FromName"]} → {r["ToName"]}",
                CoachSeat = $"Toa {r["coach_number"]} · Chỗ {r["seat_number"]}"
            });
        }
        return rows;
    }

    public void UpdatePassengers(int bookingId, List<PassengerRow> rows)
    {
        var sched = new ScheduleRepository();
        decimal total = 0;
        foreach (var r in rows)
        {
            var pt = sched.GetPassengerType(r.PassengerTypeId);
            decimal discount = pt?.DiscountPercent ?? 0;
            decimal original = Convert.ToDecimal(Db.Scalar(
                "SELECT original_price FROM tickets WHERE ticket_id=@id", Db.P("@id", r.TicketId)));
            decimal discountAmount = Math.Round(original * discount / 100m, 0);
            decimal final = original - discountAmount;

            Db.Execute(@"UPDATE tickets SET passenger_type_id=@pt, passenger_name=@n, id_number=@idn,
                         discount_amount=@d, final_price=@f WHERE ticket_id=@tid",
                Db.P("@pt", r.PassengerTypeId), Db.P("@n", (r.PassengerName ?? "").Trim()),
                Db.P("@idn", (r.IdNumber ?? "").Trim()), Db.P("@d", discountAmount),
                Db.P("@f", final), Db.P("@tid", r.TicketId));
            total += final;
        }
        Db.Execute("UPDATE bookings SET total_amount = @t WHERE booking_id = @id",
            Db.P("@t", total), Db.P("@id", bookingId));
    }

    public bool Pay(int bookingId, string method)
    {
        var b = GetById(bookingId);
        if (b == null || b.Status != "holding") return false;
        ExpireHeld();
        b = GetById(bookingId);
        if (b.Status != "holding") return false;

        decimal amount = Convert.ToDecimal(Db.Scalar(
            "SELECT total_amount FROM bookings WHERE booking_id=@id", Db.P("@id", bookingId)));
        string txref = "SIM-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

        Db.Execute(@"INSERT INTO payments (booking_id, method, amount, transaction_ref, [status], created_at, paid_at)
                     VALUES (@b,@m,@a,@r,'success',SYSDATETIME(),SYSDATETIME())",
            Db.P("@b", bookingId), Db.P("@m", method), Db.P("@a", amount), Db.P("@r", txref));
        Db.Execute("UPDATE bookings SET [status]='paid' WHERE booking_id=@id", Db.P("@id", bookingId));
        Db.Execute("UPDATE tickets SET [status]='paid' WHERE booking_id=@id", Db.P("@id", bookingId));
        return true;
    }

    // ===== Lịch sử =====
    public List<Booking> GetByUser(int userId)
    {
        ExpireHeld();
        return Db.Map<Booking>(Db.Query(
            "SELECT * FROM bookings WHERE user_id = @u ORDER BY created_at DESC", Db.P("@u", userId)));
    }

    public List<Booking> GetAll(string status = null)
    {
        ExpireHeld();
        string sql = "SELECT * FROM bookings";
        if (!string.IsNullOrWhiteSpace(status)) sql += " WHERE [status] = @s";
        sql += " ORDER BY created_at DESC";
        return Db.Map<Booking>(Db.Query(sql, Db.P("@s", status ?? "")));
    }

    public List<Payment> GetPayments(int bookingId)
        => Db.Map<Payment>(Db.Query(
            "SELECT * FROM payments WHERE booking_id = @id ORDER BY created_at DESC", Db.P("@id", bookingId)));

    public TicketDetailViewModel GetTicket(int ticketId)
    {
        var t = Db.MapOne<Ticket>(Db.Query("SELECT * FROM tickets WHERE ticket_id = @id", Db.P("@id", ticketId)));
        if (t == null) return null;
        var dt = Db.Query(@"
            SELECT tn.train_code, s.seat_number, c.coach_number, ct.type_name AS CoachTypeName,
                   dep.station_name AS FromStationName, arr.station_name AS ToStationName,
                   pt.type_name AS PassengerTypeName,
                   dep2.departure_time AS DepartureTime, arr2.arrival_time AS ArrivalTime,
                   ISNULL(tr.delay_minutes,0) AS DelayMinutes,
                   CASE WHEN tr.[status]='cancelled' THEN 1 ELSE 0 END AS TripIsCancelled,
                   tr.cancel_reason AS CancelReason, rt.route_name AS RouteName
            FROM tickets tk
            JOIN trips tr ON tr.trip_id = tk.trip_id
            JOIN trains tn ON tn.train_id = tr.train_id
            JOIN seats s ON s.seat_id = tk.seat_id
            JOIN coaches c ON c.coach_id = s.coach_id
            JOIN coach_types ct ON ct.coach_type_id = c.coach_type_id
            JOIN stations dep ON dep.station_id = tk.from_station_id
            JOIN stations arr ON arr.station_id = tk.to_station_id
            JOIN passenger_types pt ON pt.passenger_type_id = tk.passenger_type_id
            JOIN trip_stops dep2 ON dep2.trip_id = tk.trip_id AND dep2.station_id = tk.from_station_id
            JOIN trip_stops arr2 ON arr2.trip_id = tk.trip_id AND arr2.station_id = tk.to_station_id
            LEFT JOIN routes rt ON rt.route_id = tn.route_id
            WHERE tk.ticket_id = @id", Db.P("@id", ticketId));
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new TicketDetailViewModel
        {
            Ticket = t,
            TrainCode = row["train_code"].ToString(),
            SeatNumber = row["seat_number"].ToString(),
            CoachNumber = Convert.ToInt32(row["coach_number"]),
            CoachTypeName = row["CoachTypeName"].ToString(),
            FromStationName = row["FromStationName"].ToString(),
            ToStationName = row["ToStationName"].ToString(),
            PassengerTypeName = row["PassengerTypeName"].ToString(),
            DepartureTime = row["DepartureTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["DepartureTime"]),
            ArrivalTime = row["ArrivalTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["ArrivalTime"]),
            DelayMinutes = Convert.ToInt32(row["DelayMinutes"]),
            TripIsCancelled = Convert.ToInt32(row["TripIsCancelled"]) == 1,
            CancelReason = row["CancelReason"] == DBNull.Value ? null : row["CancelReason"].ToString(),
            RouteName = row["RouteName"] == DBNull.Value ? null : row["RouteName"].ToString()
        };
    }

    public BookingDetailViewModel GetBookingDetail(int bookingId)
    {
        var b = GetById(bookingId);
        if (b == null) return null;
        var vm = new BookingDetailViewModel { Booking = b, Payments = GetPayments(bookingId) };
        if (b.CouponId.HasValue)
        {
            var code = Db.Scalar("SELECT code FROM coupons WHERE coupon_id=@c", Db.P("@c", b.CouponId.Value));
            vm.CouponCode = code == null || code == DBNull.Value ? null : code.ToString();
        }
        vm.DiscountAmount = b.DiscountAmount;
        var tids = Db.Query("SELECT ticket_id FROM tickets WHERE booking_id = @id", Db.P("@id", bookingId));
        foreach (DataRow r in tids.Rows)
        {
            var d = GetTicket(Convert.ToInt32(r["ticket_id"]));
            if (d != null) vm.Tickets.Add(d);
        }
        return vm;
    }

    // ===== Hủy vé / hoàn tiền =====
    public (bool ok, string msg, decimal fee, decimal refund) RequestRefund(int ticketId, int userId)
    {
        var t = Db.MapOne<Ticket>(Db.Query("SELECT * FROM tickets WHERE ticket_id = @id", Db.P("@id", ticketId)));
        if (t == null) return (false, "Không tìm thấy vé.", 0, 0);
        var b = GetById(t.BookingId);
        if (b == null || b.UserId != userId) return (false, "Bạn không có quyền hủy vé này.", 0, 0);
        if (t.Status != "paid") return (false, "Vé không ở trạng thái có thể hủy.", 0, 0);

        var depObj = Db.Scalar("SELECT departure_time FROM trip_stops WHERE trip_id=@t AND station_id=@s",
            Db.P("@t", t.TripId), Db.P("@s", t.FromStationId));
        DateTime depart = (depObj == null || depObj == DBNull.Value)
            ? DateTime.Now.AddHours(1) : Convert.ToDateTime(depObj);
        double hours = (depart - DateTime.Now).TotalHours;
        if (hours < 0) return (false, "Chuyến đã khởi hành, không thể hủy vé.", 0, 0);

        decimal feePercent = Services.AppSettings.GetCancelFeePercent(hours);
        decimal fee = Math.Round(t.FinalPrice * feePercent / 100m, 0);
        decimal refund = t.FinalPrice - fee;

        var payObj = Db.Scalar(
            "SELECT TOP 1 payment_id FROM payments WHERE booking_id=@b AND [status]='success' ORDER BY paid_at DESC",
            Db.P("@b", t.BookingId));
        int? payId = (payObj == null || payObj == DBNull.Value) ? (int?)null : Convert.ToInt32(payObj);

        Db.Execute(@"INSERT INTO refunds (ticket_id, payment_id, reason, refund_fee, refund_amount, [status], requested_at)
                     VALUES (@t,@p,N'Hủy vé',@fee,@amt,'requested',SYSDATETIME())",
            Db.P("@t", ticketId), Db.P("@p", (object)payId ?? DBNull.Value),
            Db.P("@fee", fee), Db.P("@amt", refund));
        Db.Execute("UPDATE tickets SET [status]='cancelled' WHERE ticket_id=@id", Db.P("@id", ticketId));
        return (true, "Đã gửi yêu cầu hủy vé.", fee, refund);
    }

    public List<RefundDetailViewModel> GetRefunds()
    {
        var list = new List<RefundDetailViewModel>();
        var dt = Db.Query(@"
            SELECT rf.*, tk.ticket_code, tk.passenger_name, t.train_code,
                   dep.station_name AS FromStationName, arr.station_name AS ToStationName,
                   u.email AS UserEmail
            FROM refunds rf
            JOIN tickets tk ON tk.ticket_id = rf.ticket_id
            JOIN trips tr ON tr.trip_id = tk.trip_id
            JOIN trains t ON t.train_id = tr.train_id
            JOIN stations dep ON dep.station_id = tk.from_station_id
            JOIN stations arr ON arr.station_id = tk.to_station_id
            JOIN bookings b ON b.booking_id = tk.booking_id
            JOIN users u ON u.user_id = b.user_id
            ORDER BY rf.requested_at DESC");
        foreach (DataRow row in dt.Rows)
        {
            list.Add(new RefundDetailViewModel
            {
                Refund = new Refund
                {
                    RefundId = Convert.ToInt32(row["refund_id"]),
                    TicketId = Convert.ToInt32(row["ticket_id"]),
                    PaymentId = row["payment_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["payment_id"]),
                    Reason = row["reason"].ToString(),
                    RefundFee = Convert.ToDecimal(row["refund_fee"]),
                    RefundAmount = Convert.ToDecimal(row["refund_amount"]),
                    Status = row["status"].ToString(),
                    RequestedAt = Convert.ToDateTime(row["requested_at"]),
                    ProcessedBy = row["processed_by"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["processed_by"]),
                    ProcessedAt = row["processed_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["processed_at"])
                },
                TicketCode = row["ticket_code"].ToString(),
                PassengerName = row["passenger_name"].ToString(),
                TrainCode = row["train_code"].ToString(),
                FromStationName = row["FromStationName"].ToString(),
                ToStationName = row["ToStationName"].ToString(),
                UserEmail = row["UserEmail"].ToString()
            });
        }
        return list;
    }

    public void ProcessRefund(int refundId, int adminId, bool approve)
    {
        Db.InTransaction<object>((c, tx) =>
        {
            var get = new SqlCommand(
                "SELECT ticket_id, payment_id, refund_amount FROM refunds WHERE refund_id = @id", c, tx);
            get.Parameters.AddWithValue("@id", refundId);
            int ticketId;
            int? payId;
            using (var rd = get.ExecuteReader())
            {
                if (!rd.Read()) return null;
                ticketId = Convert.ToInt32(rd["ticket_id"]);
                payId = rd["payment_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(rd["payment_id"]);
            }

            if (approve)
            {
                var up = new SqlCommand(
                    "UPDATE refunds SET [status]='completed', processed_by=@a, processed_at=SYSDATETIME() WHERE refund_id=@id", c, tx);
                up.Parameters.AddWithValue("@a", adminId);
                up.Parameters.AddWithValue("@id", refundId);
                up.ExecuteNonQuery();

                var upT = new SqlCommand("UPDATE tickets SET [status]='cancelled' WHERE ticket_id=@t", c, tx);
                upT.Parameters.AddWithValue("@t", ticketId);
                upT.ExecuteNonQuery();

                if (payId.HasValue)
                {
                    var upP = new SqlCommand("UPDATE payments SET [status]='refunded' WHERE payment_id=@p", c, tx);
                    upP.Parameters.AddWithValue("@p", payId.Value);
                    upP.ExecuteNonQuery();
                }
            }
            else
            {
                var up = new SqlCommand(
                    "UPDATE refunds SET [status]='rejected', processed_by=@a, processed_at=SYSDATETIME() WHERE refund_id=@id", c, tx);
                up.Parameters.AddWithValue("@a", adminId);
                up.Parameters.AddWithValue("@id", refundId);
                up.ExecuteNonQuery();

                var upT = new SqlCommand("UPDATE tickets SET [status]='paid' WHERE ticket_id=@t", c, tx);
                upT.Parameters.AddWithValue("@t", ticketId);
                upT.ExecuteNonQuery();
            }
            return null;
        });
    }
}
