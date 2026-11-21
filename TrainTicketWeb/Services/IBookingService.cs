using System.Data;
using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;
using TrainTicketWeb.ViewModels;

namespace TrainTicketWeb.Services;

public interface IBookingService
{
    void ExpireHeld();
    List<SeatMapCoachViewModel> GetSeatMap(int tripId, int fromId, int toId);
    (int bookingId, string error) CreateBooking(int userId, string tripType, string email, string phone, List<SeatPick> picks);
    Booking GetBooking(int id);
    List<PassengerRow> GetBookingPassengers(int bookingId);
    void UpdatePassengers(int bookingId, List<PassengerRow> rows);
    bool Pay(int bookingId, string method);
    List<Booking> GetMyBookings(int userId);
    List<Booking> GetAll(string status = null, string keyword = null);
    List<Payment> GetPayments(int bookingId);
    TicketDetailViewModel GetTicketDetail(int ticketId);
    BookingDetailViewModel GetBookingDetail(int bookingId);
    (bool ok, string msg, decimal fee, decimal refund) RequestRefund(int ticketId, int userId);
    List<RefundDetailViewModel> GetRefunds();
    void ProcessRefund(int refundId, int adminId, bool approve);
}

public class BookingService : IBookingService
{
    private readonly IBookingRepository _repo;

    public BookingService(IBookingRepository repo) => _repo = repo;

    public void ExpireHeld() => _repo.ExpireHeld();

    public List<SeatMapCoachViewModel> GetSeatMap(int tripId, int fromId, int toId) => _repo.GetSeatMap(tripId, fromId, toId);

    public (int bookingId, string error) CreateBooking(int userId, string tripType, string email, string phone, List<SeatPick> picks)
    {
        try
        {
            return _repo.Create(userId, tripType, email, phone, picks);
        }
        catch (Exception ex)
        {
            return (0, "Lỗi khi đặt vé: " + ex.Message);
        }
    }

    public Booking GetBooking(int id) => _repo.GetById(id);

    public List<PassengerRow> GetBookingPassengers(int bookingId) => _repo.GetPassengers(bookingId);

    public void UpdatePassengers(int bookingId, List<PassengerRow> rows) => _repo.UpdatePassengers(bookingId, rows);

    public bool Pay(int bookingId, string method) => _repo.Pay(bookingId, method);

    public List<Booking> GetMyBookings(int userId) => _repo.GetByUser(userId);

    public List<Booking> GetAll(string status = null, string keyword = null) => _repo.GetAll(status, keyword);

    public List<Payment> GetPayments(int bookingId) => _repo.GetPayments(bookingId);

    public TicketDetailViewModel GetTicketDetail(int ticketId) => _repo.GetTicket(ticketId);

    public BookingDetailViewModel GetBookingDetail(int bookingId) => _repo.GetBookingDetail(bookingId);

    public (bool ok, string msg, decimal fee, decimal refund) RequestRefund(int ticketId, int userId)
        => _repo.RequestRefund(ticketId, userId);

    public List<RefundDetailViewModel> GetRefunds() => _repo.GetRefunds();

    public void ProcessRefund(int refundId, int adminId, bool approve) => _repo.ProcessRefund(refundId, adminId, approve);
}
