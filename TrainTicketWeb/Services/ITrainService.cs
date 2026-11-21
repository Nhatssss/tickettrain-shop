using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;

namespace TrainTicketWeb.Services;

public interface ITrainService
{
    List<Train> GetTrains();
    Train GetTrain(int id);
    void SaveTrain(int id, string code, int routeId, bool active);
    List<CoachType> GetCoachTypes();
    void SaveCoachType(int id, string name, int capacity, string description);
    List<Coach> GetCoaches(int trainId);
    List<Seat> GetSeats(int coachId);
    void ConfigureTrain(int trainId, List<(int coachTypeId, int count)> config);
    int GetCoachTypeOfSeat(int seatId);
}

public class TrainService : ITrainService
{
    private readonly ITrainRepository _repo;

    public TrainService(ITrainRepository repo) => _repo = repo;

    public List<Train> GetTrains() => _repo.GetAll();

    public Train GetTrain(int id) => _repo.GetById(id);

    public void SaveTrain(int id, string code, int routeId, bool active)
        => _repo.Save(new Train { TrainId = id, TrainCode = code ?? "", RouteId = routeId, IsActive = active });

    public List<CoachType> GetCoachTypes() => _repo.GetCoachTypes();

    public void SaveCoachType(int id, string name, int capacity, string description)
        => _repo.SaveCoachType(new CoachType
        {
            CoachTypeId = id,
            TypeName = name ?? "",
            Capacity = capacity,
            Description = description
        });

    public List<Coach> GetCoaches(int trainId) => _repo.GetCoaches(trainId);

    public List<Seat> GetSeats(int coachId) => _repo.GetSeats(coachId);

    public void ConfigureTrain(int trainId, List<(int coachTypeId, int count)> config)
    {
        if (config == null || config.Count == 0) return;
        _repo.ConfigureTrain(trainId, config);
    }

    public int GetCoachTypeOfSeat(int seatId) => _repo.GetCoachTypeOfSeat(seatId);
}
