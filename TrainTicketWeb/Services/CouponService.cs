using TrainTicketWeb.Filters;
using TrainTicketWeb.Data;
using TrainTicketWeb.Hubs;
using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;
using TrainTicketWeb.Services;
using TrainTicketWeb.ViewModels;

namespace TrainTicketWeb.Services;

public interface ICouponService
{
    List<Coupon> GetAll();
    List<Coupon> GetActive();
    Coupon GetByCode(string code);
    (bool ok, string error, int id) Save(CouponFormViewModel form);
    void SetActive(int id, bool active);
    void Delete(int id);
    CouponApplyResult Apply(string code, int userId, int bookingId);
    decimal Subtotal(int bookingId);
}

public class CouponService : ICouponService
{
    private readonly ICouponRepository _repo;

    public CouponService(ICouponRepository repo) => _repo = repo;

    public List<Coupon> GetAll() => _repo.GetAll();
    public List<Coupon> GetActive() => _repo.GetActive();
    public Coupon GetByCode(string code) => _repo.GetByCode(code);

    public (bool ok, string error, int id) Save(CouponFormViewModel f)
    {
        if (string.IsNullOrWhiteSpace(f.Code)) return (false, "Vui lòng nhập mã.", 0);
        string code = f.Code.Trim().ToUpperInvariant();
        if (code.Length < 3) return (false, "Mã phải từ 3 ký tự.", 0);
        if (code.Any(ch => !char.IsLetterOrDigit(ch))) return (false, "Mã chỉ gồm chữ và số.", 0);
        if (f.DiscountValue <= 0) return (false, "Mức giảm phải lớn hơn 0.", 0);
        if (f.DiscountType == "percent" && f.DiscountValue > 100)
            return (false, "Mức giảm phần trăm không được quá 100%.", 0);
        if (f.PerUserLimit < 1) return (false, "Giới hạn mỗi khách phải từ 1 lần.", 0);
        if (f.ValidTo.HasValue && f.ValidTo.Value.Date < f.ValidFrom.Date)
            return (false, "Ngày kết thúc phải sau ngày bắt đầu.", 0);

        var exist = _repo.GetByCode(code);
        if (exist != null && exist.CouponId != f.CouponId)
            return (false, $"Mã {code} đã tồn tại.", 0);

        int id = _repo.Save(new Coupon
        {
            CouponId = f.CouponId,
            Code = code,
            Description = f.Description,
            DiscountType = f.DiscountType,
            DiscountValue = f.DiscountValue,
            MaxDiscount = f.MaxDiscount,
            MinOrder = f.MinOrder,
            MaxUses = f.MaxUses,
            PerUserLimit = f.PerUserLimit,
            ValidFrom = f.ValidFrom,
            ValidTo = f.ValidTo,
            IsActive = f.IsActive
        });
        return (true, null, id);
    }

    public void SetActive(int id, bool active) => _repo.SetActive(id, active);
    public void Delete(int id) => _repo.Delete(id);

    public decimal Subtotal(int bookingId)
        => Convert.ToDecimal(Db.Scalar(
            "SELECT ISNULL(SUM(final_price),0) FROM tickets WHERE booking_id=@b AND [status] IN ('holding','paid')",
            Db.P("@b", bookingId)));

    public CouponApplyResult Apply(string code, int userId, int bookingId)
    {
        decimal subtotal = Subtotal(bookingId);
        var v = _repo.Validate(code, userId, subtotal);
        if (!v.ok) return new CouponApplyResult { Ok = false, Error = v.error, Subtotal = subtotal, Total = subtotal };

        return new CouponApplyResult
        {
            Ok = true,
            Code = code.Trim().ToUpperInvariant(),
            Discount = v.discount,
            Subtotal = subtotal,
            Total = subtotal - v.discount
        };
    }
}
