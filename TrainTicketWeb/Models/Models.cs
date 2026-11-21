namespace TrainTicketWeb.Models;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }

    // Khóa tài khoản (ban) — không có ngày hết hạn, admin phải mở khóa thủ công
    public string LockedReason { get; set; }
    public DateTime? LockedAt { get; set; }

    public bool IsLocked => Status == "locked";
}

public class Station
{
    public int StationId { get; set; }
    public string StationCode { get; set; }
    public string StationName { get; set; }
    public string Province { get; set; }
    public bool IsActive { get; set; }
}

public class Route
{
    public int RouteId { get; set; }
    public string RouteName { get; set; }
    public int StartStationId { get; set; }
    public int EndStationId { get; set; }
    public string StartStationName { get; set; }
    public string EndStationName { get; set; }
}

public class RouteStation
{
    public int RouteId { get; set; }
    public int StationId { get; set; }
    public string StationName { get; set; }
    public int StopOrder { get; set; }
    public decimal DistanceKm { get; set; }
}

public class Train
{
    public int TrainId { get; set; }
    public string TrainCode { get; set; }
    public int RouteId { get; set; }
    public string RouteName { get; set; }
    public bool IsActive { get; set; }
}

public class CoachType
{
    public int CoachTypeId { get; set; }
    public string TypeName { get; set; }
    public int Capacity { get; set; }
    public string Description { get; set; }
}

public class Coach
{
    public int CoachId { get; set; }
    public int TrainId { get; set; }
    public int CoachNumber { get; set; }
    public int CoachTypeId { get; set; }
    public string TypeName { get; set; }
    public int Capacity { get; set; }
}

public class Seat
{
    public int SeatId { get; set; }
    public int CoachId { get; set; }
    public string SeatNumber { get; set; }
    public int? CompartmentNo { get; set; }
    public byte? BerthLevel { get; set; }
    public bool IsBooked { get; set; }
}

public class Trip
{
    public int TripId { get; set; }
    public int TrainId { get; set; }
    public string TrainCode { get; set; }
    public string RouteName { get; set; }
    public int RouteId { get; set; }
    public DateTime DepartureDate { get; set; }
    public string Status { get; set; }

    // Hoãn / hủy chuyến
    public int DelayMinutes { get; set; }
    public string CancelReason { get; set; }

    public bool IsDelayed => DelayMinutes > 0;
    public bool IsCancelled => Status == "cancelled";
}

public class TripStop
{
    public int TripId { get; set; }
    public int StationId { get; set; }
    public string StationName { get; set; }
    public int StopOrder { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public DateTime? DepartureTime { get; set; }
}

public class Fare
{
    public int FareId { get; set; }
    public int FromStationId { get; set; }
    public int ToStationId { get; set; }
    public int CoachTypeId { get; set; }
    public decimal BasePrice { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class PassengerType
{
    public int PassengerTypeId { get; set; }
    public string TypeName { get; set; }
    public decimal DiscountPercent { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string IdLabel { get; set; }
    public bool IsActive { get; set; }
}

public class Booking
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; }
    public int UserId { get; set; }
    public string TripType { get; set; }
    public string ContactEmail { get; set; }
    public string ContactPhone { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
    public DateTime HoldExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Mã giảm giá đã áp
    public int? CouponId { get; set; }
    public decimal DiscountAmount { get; set; }
}

public class Ticket
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; }
    public int BookingId { get; set; }
    public int TripId { get; set; }
    public int SeatId { get; set; }
    public int FromStationId { get; set; }
    public int ToStationId { get; set; }
    public string Leg { get; set; }
    public int PassengerTypeId { get; set; }
    public string PassengerName { get; set; }
    public string IdNumber { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalPrice { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Payment
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public string Method { get; set; }
    public decimal Amount { get; set; }
    public string TransactionRef { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class Refund
{
    public int RefundId { get; set; }
    public int TicketId { get; set; }
    public int? PaymentId { get; set; }
    public string Reason { get; set; }
    public decimal RefundFee { get; set; }
    public decimal RefundAmount { get; set; }
    public string Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public int? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
