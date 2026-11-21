using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;

namespace TrainTicketWeb.Services;

public interface IUserService
{
    List<User> GetAll(string keyword = null);
    User GetById(int id);
    User GetByEmail(string email);
    void SetStatus(int userId, string status);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repo;

    public UserService(IUserRepository repo) => _repo = repo;

    public List<User> GetAll(string keyword = null) => _repo.GetAll(keyword);

    public User GetById(int id) => _repo.GetById(id);

    public User GetByEmail(string email) => _repo.GetByEmail(email);

    public void SetStatus(int userId, string status) => _repo.SetStatus(userId, status);
}
