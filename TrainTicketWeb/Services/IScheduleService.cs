using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;
using TrainTicketWeb.ViewModels;

namespace TrainTicketWeb.Services;

public interface IScheduleService
{
    Trip GetTrip(int id);
    List<Trip> GetTrips();
    List<TripStop> GetTripStops(int tripId);
    int GetStopOrder(int tripId, int stationId);
    DateTime? GetDepartureTime(int tripId, int stationId);
    int CreateTrip(int trainId, DateTime date, TimeSpan baseTime);
    void SetTripStatus(int tripId, string status);
    List<Fare> GetFares();
    decimal GetFare(int fromId, int toId, int coachTypeId);
    decimal GetMinFare(int fromId, int toId);
    void SaveFare(int id, int fromId, int toId, int coachTypeId, decimal price, DateTime effectiveFrom, DateTime? effectiveTo);
    List<PassengerType> GetPassengerTypes(bool activeOnly = false);
    PassengerType GetPassengerType(int id);
    void SavePassengerType(int id, string name, decimal discount, int? minAge, int? maxAge, string idLabel, bool active);
    List<TripSearchResultViewModel> Search(int fromId, int toId, DateTime date);
}

public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _repo;
    private readonly IBookingService _booking;

    public ScheduleService(IScheduleRepository repo, IBookingService booking)
    {
        _repo = repo;
        _booking = booking;
    }

    public Trip GetTrip(int id) => _repo.GetTrip(id);

    public List<Trip> GetTrips() => _repo.GetTrips();

    public List<TripStop> GetTripStops(int tripId) => _repo.GetTripStops(tripId);

    public int GetStopOrder(int tripId, int stationId) => _repo.GetStopOrder(tripId, stationId);

    public DateTime? GetDepartureTime(int tripId, int stationId)
    {
        var stop = _repo.GetTripStops(tripId).FirstOrDefault(x => x.StationId == stationId);
        return stop?.DepartureTime;
    }

    public int CreateTrip(int trainId, DateTime date, TimeSpan baseTime) => _repo.CreateTrip(trainId, date, baseTime);

    public void SetTripStatus(int tripId, string status) => _repo.SetTripStatus(tripId, status);

    public List<Fare> GetFares() => _repo.GetFares();

    public decimal GetFare(int fromId, int toId, int coachTypeId) => _repo.GetFare(fromId, toId, coachTypeId);

    public decimal GetMinFare(int fromId, int toId) => _repo.GetMinFare(fromId, toId);

    public void SaveFare(int id, int fromId, int toId, int coachTypeId, decimal price, DateTime effectiveFrom, DateTime? effectiveTo)
        => _repo.SaveFare(new Fare
        {
            FareId = id,
            FromStationId = fromId,
            ToStationId = toId,
            CoachTypeId = coachTypeId,
            BasePrice = price,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        });

    public List<PassengerType> GetPassengerTypes(bool activeOnly = false) => _repo.GetPassengerTypes(activeOnly);

    public PassengerType GetPassengerType(int id) => _repo.GetPassengerType(id);

    public void SavePassengerType(int id, string name, decimal discount, int? minAge, int? maxAge, string idLabel, bool active)
        => _repo.SavePassengerType(new PassengerType
        {
            PassengerTypeId = id,
            TypeName = name ?? "",
            DiscountPercent = discount,
            MinAge = minAge,
            MaxAge = maxAge,
            IdLabel = idLabel,
            IsActive = active
        });

    // ===== Tìm chuyến: Repository → ViewModel =====
    public List<TripSearchResultViewModel> Search(int fromId, int toId, DateTime date)
    {
        _booking.ExpireHeld();
        decimal minFare = _repo.GetMinFare(fromId, toId);
        var rows = _repo.SearchTrips(fromId, toId, date);

        var list = new List<TripSearchResultViewModel>();
        foreach (var r in rows)
        {
            var vm = new TripSearchResultViewModel
            {
                TripId = r.TripId,
                TrainId = r.TrainId,
                TrainCode = r.TrainCode,
                RouteId = r.RouteId,
                RouteName = r.RouteName,
                DepartureDate = r.DepartureDate,
                DepartureTime = r.DepartureTime,
                ArrivalTime = r.ArrivalTime,
                AvailableSeats = r.AvailableSeats,
                MinPrice = minFare,
                DelayMinutes = r.DelayMinutes
            };
            vm.Duration = (r.ArrivalTime.HasValue && r.DepartureTime.HasValue)
                ? r.ArrivalTime.Value - r.DepartureTime.Value : TimeSpan.Zero;
            list.Add(vm);
        }
        return list;
    }
}
