using System.Data;
using TrainTicketWeb.Data;

namespace TrainTicketWeb.Models;

// ===== Mã giảm giá =====
public class Coupon
{
    public int CouponId { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public string DiscountType { get; set; }   // percent | amount
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal MinOrder { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public int PerUserLimit { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Tien ich hien thi
    public string DiscountLabel =>
        DiscountType == "percent"
            ? $"{DiscountValue:0.##}%"
            : $"{DiscountValue:N0} ₫";

    public bool IsExpired => ValidTo.HasValue && ValidTo.Value.Date < DateTime.Today;
    public bool IsOutOfUses => MaxUses.HasValue && UsedCount >= MaxUses.Value;
}

// ===== Thông báo =====
public class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; }       // trip_delayed | trip_cancelled | refund | coupon | system
    public string Title { get; set; }
    public string Body { get; set; }
    public string LinkUrl { get; set; }
    public int? RefTripId { get; set; }
    public string RefCouponCode { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    // Nhan hien thi cho UI
    public string TypeLabel => Type switch
    {
        "trip_delayed" => "Hoãn chuyến",
        "trip_cancelled" => "Hủy chuyến",
        "refund" => "Hoàn tiền",
        "coupon" => "Ưu đãi",
        _ => "Hệ thống"
    };

    public string TimeAgo
    {
        get
        {
            var d = DateTime.Now - CreatedAt;
            if (d.TotalMinutes < 1) return "vừa xong";
            if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes} phút trước";
            if (d.TotalHours < 24) return $"{(int)d.TotalHours} giờ trước";
            if (d.TotalDays < 30) return $"{(int)d.TotalDays} ngày trước";
            return CreatedAt.ToString("dd/MM/yyyy");
        }
    }
}
