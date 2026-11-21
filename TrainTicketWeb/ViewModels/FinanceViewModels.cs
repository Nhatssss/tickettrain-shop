namespace TrainTicketWeb.ViewModels;

// ===== Quỹ quản trị (C1: chỉ hiển thị số liệu thật, không ví ảo) =====
public class FinanceSummary
{
    public decimal GrossIncome { get; set; }        // tổng tiền khách đã trả
    public decimal TotalRefunded { get; set; }      // tổng đã hoàn
    public decimal TotalDiscount { get; set; }      // tổng đã giảm qua mã
    public decimal Fund { get; set; }               // quỹ hiện tại = thu - hoàn
    public decimal PendingRefundAmount { get; set; }
    public int PendingRefundCount { get; set; }
    public int PaidBookings { get; set; }
    public int PaidTickets { get; set; }
    public int CustomerCount { get; set; }
    public int LockedCustomerCount { get; set; }

    public double RefundRate => GrossIncome > 0 ? (double)(TotalRefunded / GrossIncome) * 100 : 0;
}

public class CustomerFinance
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Status { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRefunded { get; set; }
    public decimal TotalDiscountSaved { get; set; }
    public decimal PendingRefund { get; set; }
    public decimal Balance { get; set; }
    public int BookingCount { get; set; }
}

// ===== Trang quỹ của admin =====
public class FinancePageViewModel
{
    public FinanceSummary Summary { get; set; }
    public List<CustomerFinance> Customers { get; set; } = new();
}

// ===== Áp mã giảm giá ở trang thanh toán =====
public class CouponApplyResult
{
    public bool Ok { get; set; }
    public string Error { get; set; }
    public string Code { get; set; }
    public decimal Discount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
}

// ===== Tạo/sửa mã giảm giá từ form admin =====
public class CouponFormViewModel
{
    public int CouponId { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public string DiscountType { get; set; } = "percent";
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal MinOrder { get; set; }
    public int? MaxUses { get; set; }
    public int PerUserLimit { get; set; } = 1;
    public DateTime ValidFrom { get; set; } = DateTime.Today;
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}
