using System.Data;
using Microsoft.Data.SqlClient;
using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;
using TrainTicketWeb.ViewModels;

namespace TrainTicketWeb.Services;

public interface IStationService
{
    List<Station> GetStations(bool activeOnly = false);
    Station GetStation(int id);
    void SaveStation(int id, string code, string name, string province, bool active);
}

public class StationService : IStationService
{
    private readonly IStationRepository _repo;

    public StationService(IStationRepository repo) => _repo = repo;

    public List<Station> GetStations(bool activeOnly = false) => _repo.GetAll(activeOnly);

    public Station GetStation(int id) => _repo.GetById(id);

    public void SaveStation(int id, string code, string name, string province, bool active)
        => _repo.Save(new Station
        {
            StationId = id,
            StationCode = code ?? "",
            StationName = name ?? "",
            Province = province,
            IsActive = active
        });
}
