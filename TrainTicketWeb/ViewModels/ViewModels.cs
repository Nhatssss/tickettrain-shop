using TrainTicketWeb.Models;
using Route = TrainTicketWeb.Models.Route;

namespace TrainTicketWeb.ViewModels;

// ===== Tìm chuyến =====
public class SearchViewModel
{
    public int FromStationId { get; set; }
    public int ToStationId { get; set; }
    public string TripType { get; set; } = "one_way";
    public DateTime? DepartureDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public int AdultCount { get; set; } = 1;
    public int ChildCount { get; set; }
    public int StudentCount { get; set; }
    public int SeniorCount { get; set; }
    public List<Station> Stations { get; set; } = new();
    public string Error { get; set; }
    public int TotalPassengers => AdultCount + ChildCount + StudentCount + SeniorCount;
}

public class TripSearchResultViewModel
{
    public int TripId { get; set; }
    public int TrainId { get; set; }
    public string TrainCode { get; set; }
    public int RouteId { get; set; }
    public string RouteName { get; set; }
    public DateTime DepartureDate { get; set; }
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int AvailableSeats { get; set; }
    public decimal MinPrice { get; set; }

    // Hoãn chuyến
    public int DelayMinutes { get; set; }
    public bool IsDelayed => DelayMinutes > 0;
    public DateTime? NewDepartureTime => DepartureTime?.AddMinutes(DelayMinutes);
}

public class SearchResultsViewModel
{
    public SearchViewModel Query { get; set; }
    public List<TripSearchResultViewModel> Outbound { get; set; } = new();
    public List<TripSearchResultViewModel> Return { get; set; } = new();
    public string FromStationName { get; set; }
    public string ToStationName { get; set; }
}

// ===== Chọn chỗ =====
public class SeatMapSeatViewModel
{
    public int SeatId { get; set; }
    public string SeatNumber { get; set; }
    public int? CompartmentNo { get; set; }
    public byte? BerthLevel { get; set; }
    public string Status { get; set; } // free | held | sold
}

public class SeatMapCoachViewModel
{
    public int CoachId { get; set; }
    public int CoachNumber { get; set; }
    public int CoachTypeId { get; set; }
    public string TypeName { get; set; }
    public List<SeatMapSeatViewModel> Seats { get; set; } = new();
}

public class SeatSelectViewModel
{
    public int TripId { get; set; }
    public string TrainCode { get; set; }
    public string RouteName { get; set; }
    public DateTime DepartureDate { get; set; }
    public int FromStationId { get; set; }
    public string FromStationName { get; set; }
    public int ToStationId { get; set; }
    public string ToStationName { get; set; }
    public string Leg { get; set; }
    public int PassengerCount { get; set; }
    public List<SeatMapCoachViewModel> Coaches { get; set; } = new();
}

public class SeatPick
{
    public int TripId { get; set; }
    public int SeatId { get; set; }
    public int FromId { get; set; }
    public int ToId { get; set; }
    public string Leg { get; set; }
    public int CoachTypeId { get; set; }
}

// ===== Hành khách =====
public class PassengerRow
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; }
    public string Leg { get; set; }
    public string TripLabel { get; set; }
    public string CoachSeat { get; set; }
    public int CoachTypeId { get; set; }
    public int PassengerTypeId { get; set; }
    public string PassengerName { get; set; }
    public string IdNumber { get; set; }
    public decimal OriginalPrice { get; set; }
}

public class PassengersViewModel
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; }
    public string TripType { get; set; }
    public List<PassengerRow> Passengers { get; set; } = new();
    public List<PassengerType> PassengerTypes { get; set; } = new();
    public string Error { get; set; }
}

// ===== Vé / đơn =====
public class TicketDetailViewModel
{
    public Ticket Ticket { get; set; }
    public string TrainCode { get; set; }
    public string SeatNumber { get; set; }
    public int CoachNumber { get; set; }
    public string CoachTypeName { get; set; }
    public string FromStationName { get; set; }
    public string ToStationName { get; set; }
    public string PassengerTypeName { get; set; }
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }

    // Trạng thái chuyến (hoãn / hủy) để hiển thị cảnh báo trên vé
    public int DelayMinutes { get; set; }
    public bool TripIsCancelled { get; set; }
    public string CancelReason { get; set; }
    public string RouteName { get; set; }
}

public class BookingDetailViewModel
{
    public Booking Booking { get; set; }
    public List<TicketDetailViewModel> Tickets { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();

    // Mã giảm giá đã áp cho đơn (nếu có)
    public string CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }

    /// <summary>Tổng tiền vé trước giảm giá.</summary>
    public decimal Subtotal => Tickets.Sum(t => t.Ticket.FinalPrice) + DiscountAmount;
}

// ===== Hoàn tiền =====
public class RefundDetailViewModel
{
    public Refund Refund { get; set; }
    public string TicketCode { get; set; }
    public string PassengerName { get; set; }
    public string TrainCode { get; set; }
    public string FromStationName { get; set; }
    public string ToStationName { get; set; }
    public string UserEmail { get; set; }
}

// ===== ViewComponent: menu ga =====
public class StationMenuViewModel
{
    public List<Station> Stations { get; set; } = new();
    public int CurrentStationId { get; set; }
}

// ===== Admin: chi tiết tuyến =====
public class RouteDetailViewModel
{
    public Route Route { get; set; }
    public List<RouteStation> RouteStations { get; set; } = new();
    public List<Station> AllStations { get; set; } = new();
    public List<int> SelectedStationIds { get; set; } = new();
    public List<decimal> SelectedDistances { get; set; } = new();
}

// ===== Admin: thống kê =====
public class DashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueLast7 { get; set; }
    public decimal RevenueLast30 { get; set; }
    public int TotalBookings { get; set; }
    public int TotalTickets { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalTrips { get; set; }
    public int PendingRefunds { get; set; }
    public List<(string label, decimal value)> RevenueByRoute { get; set; } = new();
    public List<(string label, decimal value)> RevenueByDay { get; set; } = new();
    public List<(string label, double percent)> OccupancyByTrip { get; set; } = new();
}
