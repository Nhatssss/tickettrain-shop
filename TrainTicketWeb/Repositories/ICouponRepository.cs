using Microsoft.Data.SqlClient;
using TrainTicketWeb.Data;
using TrainTicketWeb.Models;

namespace TrainTicketWeb.Repositories;

public interface ICouponRepository
{
    List<Coupon> GetAll();
    List<Coupon> GetActive();
    Coupon GetByCode(string code);
    Coupon GetById(int id);
    int Save(Coupon c);
    void SetActive(int id, bool active);
    void Delete(int id);

    /// <summary>Kiem tra ma co dung duoc cho don nay khong. Tra ve (ok, thong bao loi, so tien giam).</summary>
    (bool ok, string error, decimal discount) Validate(string code, int userId, decimal orderAmount);

    /// <summary>Danh dau da dung ma (tang used_count). Goi trong transaction dat ve.</summary>
    void Consume(string code);

    /// <summary>So lan 1 khach da dung ma nay (dem qua bookings.coupon_id).</summary>
    int CountUserUsage(string code, int userId);

    decimal SumDiscountByUser(int userId);
    decimal SumDiscountAll();
}

public class CouponRepository : ICouponRepository
{
    private const string Cols = @"coupon_id, code, description, discount_type, discount_value, max_discount,
        min_order, max_uses, used_count, per_user_limit, valid_from, valid_to, is_active, created_at";

    public List<Coupon> GetAll()
        => Db.Map<Coupon>(Db.Query($"SELECT {Cols} FROM coupons ORDER BY is_active DESC, created_at DESC"));

    public List<Coupon> GetActive()
        => Db.Map<Coupon>(Db.Query(
            $@"SELECT {Cols} FROM coupons
               WHERE is_active = 1
                 AND valid_from <= CAST(SYSDATETIME() AS DATE)
                 AND (valid_to IS NULL OR valid_to >= CAST(SYSDATETIME() AS DATE))
                 AND (max_uses IS NULL OR used_count < max_uses)
               ORDER BY discount_value DESC"));

    public Coupon GetByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return Db.MapOne<Coupon>(Db.Query(
            $"SELECT {Cols} FROM coupons WHERE code = @c", Db.P("@c", code.Trim().ToUpperInvariant())));
    }

    public Coupon GetById(int id)
        => Db.MapOne<Coupon>(Db.Query($"SELECT {Cols} FROM coupons WHERE coupon_id = @id", Db.P("@id", id)));

    public int Save(Coupon c)
    {
        if (c.CouponId > 0)
        {
            Db.Execute(@"UPDATE coupons SET code=@code, description=@desc, discount_type=@dt,
                         discount_value=@dv, max_discount=@md, min_order=@mo, max_uses=@mu,
                         per_user_limit=@pul, valid_from=@vf, valid_to=@vt, is_active=@ac
                         WHERE coupon_id=@id",
                Db.P("@code", c.Code.Trim().ToUpperInvariant()), Db.P("@desc", c.Description),
                Db.P("@dt", c.DiscountType), Db.P("@dv", c.DiscountValue),
                Db.P("@md", c.MaxDiscount), Db.P("@mo", c.MinOrder),
                Db.P("@mu", c.MaxUses), Db.P("@pul", c.PerUserLimit),
                Db.P("@vf", c.ValidFrom.Date), Db.P("@vt", c.ValidTo?.Date),
                Db.P("@ac", c.IsActive), Db.P("@id", c.CouponId));
            return c.CouponId;
        }

        // Dùng chung 1 kết nối: SCOPE_IDENTITY() phải chạy trên cùng connection với INSERT,
        // nếu không Db.Scalar mở connection khác -> trả về NULL.
        using var conn = Db.Open();
        using var cmd = new SqlCommand(@"INSERT INTO coupons (code, description, discount_type, discount_value, max_discount,
                     min_order, max_uses, used_count, per_user_limit, valid_from, valid_to, is_active)
                     OUTPUT INSERTED.coupon_id
                     VALUES (@code,@desc,@dt,@dv,@md,@mo,@mu,0,@pul,@vf,@vt,@ac)", conn);
        cmd.Parameters.AddWithValue("@code", c.Code.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("@desc", (object)c.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dt", c.DiscountType ?? "percent");
        cmd.Parameters.AddWithValue("@dv", c.DiscountValue);
        cmd.Parameters.AddWithValue("@md", (object)c.MaxDiscount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@mo", c.MinOrder);
        cmd.Parameters.AddWithValue("@mu", (object)c.MaxUses ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pul", c.PerUserLimit);
        cmd.Parameters.AddWithValue("@vf", c.ValidFrom.Date);
        cmd.Parameters.AddWithValue("@vt", (object)c.ValidTo?.Date ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ac", c.IsActive);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void SetActive(int id, bool active)
        => Db.Execute("UPDATE coupons SET is_active=@a WHERE coupon_id=@id",
            Db.P("@a", active), Db.P("@id", id));

    public void Delete(int id)
        => Db.Execute("DELETE FROM coupons WHERE coupon_id=@id", Db.P("@id", id));

    public int CountUserUsage(string code, int userId)
        => Convert.ToInt32(Db.Scalar(
            @"SELECT COUNT(*) FROM bookings b
              JOIN coupons c ON c.coupon_id = b.coupon_id
              WHERE c.code = @code AND b.user_id = @u AND b.[status] <> 'expired'",
            Db.P("@code", code.Trim().ToUpperInvariant()), Db.P("@u", userId)));

    public (bool ok, string error, decimal discount) Validate(string code, int userId, decimal orderAmount)
    {
        var c = GetByCode(code);
        if (c == null) return (false, "Mã giảm giá không tồn tại.", 0);
        if (!c.IsActive) return (false, "Mã giảm giá đã bị tạm dừng.", 0);
        if (c.ValidFrom.Date > DateTime.Today) return (false, "Mã giảm giá chưa tới ngày sử dụng.", 0);
        if (c.IsExpired) return (false, "Mã giảm giá đã hết hạn.", 0);
        if (c.IsOutOfUses) return (false, "Mã giảm giá đã hết lượt sử dụng.", 0);
        if (orderAmount < c.MinOrder)
            return (false, $"Đơn hàng tối thiểu {c.MinOrder:N0} ₫ mới dùng được mã này.", 0);
        if (CountUserUsage(c.Code, userId) >= c.PerUserLimit)
            return (false, $"Bạn đã dùng mã này rồi (giới hạn {c.PerUserLimit} lần/khách).", 0);

        decimal discount = c.DiscountType == "percent"
            ? Math.Round(orderAmount * c.DiscountValue / 100m, 0)
            : c.DiscountValue;

        if (c.MaxDiscount.HasValue && discount > c.MaxDiscount.Value)
            discount = c.MaxDiscount.Value;

        if (discount > orderAmount) discount = orderAmount;
        if (discount < 0) discount = 0;

        return (true, null, discount);
    }

    public void Consume(string code)
        => Db.Execute("UPDATE coupons SET used_count = used_count + 1 WHERE code = @c",
            Db.P("@c", code.Trim().ToUpperInvariant()));

    public decimal SumDiscountByUser(int userId)
        => Convert.ToDecimal(Db.Scalar(
            "SELECT ISNULL(SUM(discount_amount),0) FROM bookings WHERE user_id=@u AND [status]='paid'",
            Db.P("@u", userId)));

    public decimal SumDiscountAll()
        => Convert.ToDecimal(Db.Scalar(
            "SELECT ISNULL(SUM(discount_amount),0) FROM bookings WHERE [status]='paid'"));
}
