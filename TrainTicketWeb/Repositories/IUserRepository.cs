using System.Data;
using TrainTicketWeb.Models;

using TrainTicketWeb.Data;

namespace TrainTicketWeb.Repositories;

public interface IUserRepository
{
    User GetByEmail(string email);
    User GetById(int id);
    void Insert(User user);
    void UpdatePassword(int userId, string passwordHash);
    void UpdateProfile(int userId, string fullName, string phone);
    List<User> GetAll(string keyword = null);
    void SetStatus(int userId, string status);
}

public class UserRepository : IUserRepository
{
    public User GetByEmail(string email)
        => Db.MapOne<User>(Db.Query("SELECT * FROM users WHERE email = @e", Db.P("@e", email)));

    public User GetById(int id)
        => Db.MapOne<User>(Db.Query("SELECT * FROM users WHERE user_id = @id", Db.P("@id", id)));

    public void Insert(User u)
        => Db.Execute(@"INSERT INTO users (full_name, email, phone, password_hash, [role], [status])
                        VALUES (@n,@e,@p,@h,@r,@s)",
            Db.P("@n", u.FullName.Trim()), Db.P("@e", u.Email.Trim()),
            Db.P("@p", (u.Phone ?? "").Trim()), Db.P("@h", u.PasswordHash),
            Db.P("@r", string.IsNullOrWhiteSpace(u.Role) ? "customer" : u.Role),
            Db.P("@s", string.IsNullOrWhiteSpace(u.Status) ? "active" : u.Status));

    public void UpdatePassword(int userId, string passwordHash)
        => Db.Execute("UPDATE users SET password_hash = @h WHERE user_id = @id",
            Db.P("@h", passwordHash), Db.P("@id", userId));

    public void UpdateProfile(int userId, string fullName, string phone)
        => Db.Execute("UPDATE users SET full_name = @n, phone = @p WHERE user_id = @id",
            Db.P("@n", fullName.Trim()), Db.P("@p", (phone ?? "").Trim()), Db.P("@id", userId));

    public List<User> GetAll(string keyword = null)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Db.Map<User>(Db.Query("SELECT * FROM users ORDER BY created_at DESC"));

        return Db.Map<User>(Db.Query(
            @"SELECT * FROM users
              WHERE full_name LIKE @kw OR email LIKE @kw OR ISNULL(phone,'') LIKE @kw
              ORDER BY created_at DESC",
            Db.P("@kw", "%" + keyword.Trim() + "%")));
    }

    public void SetStatus(int userId, string status)
        => Db.Execute("UPDATE users SET [status]=@s WHERE user_id=@id",
            Db.P("@s", status), Db.P("@id", userId));
}
