namespace TrainTicketWeb.Services;

public class CancelTier
{
    public double HoursBefore { get; set; }
    public decimal FeePercent { get; set; }
}

/// <summary>Cấu hình nghiệp vụ đọc từ appsettings.json (không viết cứng trong mã nguồn).</summary>
public static class AppSettings
{
    public static int HoldMinutes { get; private set; } = 15;
    public static List<CancelTier> CancelTiers { get; private set; } = new();

    public static void Init(IConfiguration cfg)
    {
        HoldMinutes = cfg.GetValue<int>("BusinessRules:HoldMinutes", 15);
        var tiers = cfg.GetSection("BusinessRules:CancellationPolicy").Get<List<CancelTier>>();
        CancelTiers = tiers != null && tiers.Count > 0 ? tiers : new List<CancelTier>
        {
            new CancelTier { HoursBefore = 72, FeePercent = 0 },
            new CancelTier { HoursBefore = 24, FeePercent = 10 },
            new CancelTier { HoursBefore = 6,  FeePercent = 20 },
            new CancelTier { HoursBefore = 0,  FeePercent = 30 }
        };
    }

    /// <summary>Phí hủy (%) theo số giờ trước giờ khởi hành.</summary>
    public static decimal GetCancelFeePercent(double hoursBefore)
    {
        var ordered = CancelTiers.OrderByDescending(t => t.HoursBefore).ToList();
        foreach (var t in ordered)
            if (hoursBefore >= t.HoursBefore) return t.FeePercent;
        return ordered.Count > 0 ? ordered.Last().FeePercent : 0;
    }
}
