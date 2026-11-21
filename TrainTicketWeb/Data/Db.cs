using System.Data;
using Microsoft.Data.SqlClient;

namespace TrainTicketWeb.Data;
public static class Db
{
    private static string _connStr;

    public static void Configure(IConfiguration config)
    {
        // Uu tien bien moi truong (dat trong web.config / panel hosting),
        // sau do appsettings.json. Giu nguyen ten "TrainTicketDB".
        _connStr = Environment.GetEnvironmentVariable("ConnectionStrings__TrainTicketDB");
        if (string.IsNullOrWhiteSpace(_connStr))
            _connStr = config.GetConnectionString("TrainTicketDB");

        if (string.IsNullOrWhiteSpace(_connStr))
            throw new InvalidOperationException("Thieu chuoi ket noi TrainTicketDB (ConnectionStrings:TrainTicketDB).");
    }

    /// <summary>Mở kết nối, tự thử lại khi gặp lỗi mạng tạm thời (hosting dùng chung).</summary>
    public static SqlConnection Open()
    {
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            var c = new SqlConnection(_connStr);
            try
            {
                c.Open();
                return c;
            }
            catch (SqlException ex) when (attempt < maxAttempts && IsTransient(ex))
            {
                c.Dispose();
                System.Threading.Thread.Sleep(250 * attempt);
            }
        }
    }

    /// <summary>Lỗi kết nối/không có sẵn máy chủ — đáng thử lại.</summary>
    private static bool IsTransient(SqlException ex)
    {
        // 40613: database không có sẵn, 40197/40501/4060: dịch vụ bận, bị ngắt
        foreach (var n in new[] { -2, 258, 233, 4060, 40197, 40501, 40613, 10053, 10054, 10060 })
            if (ex.Number == n) return true;
        return false;
    }

    public static DataTable Query(string sql, params SqlParameter[] ps)
    {
        using var c = Open();
        using var cmd = new SqlCommand(sql, c);
        if (ps != null) cmd.Parameters.AddRange(ps);
        using var da = new SqlDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);
        return dt;
    }

    public static int Execute(string sql, params SqlParameter[] ps)
    {
        using var c = Open();
        using var cmd = new SqlCommand(sql, c);
        if (ps != null) cmd.Parameters.AddRange(ps);
        return cmd.ExecuteNonQuery();
    }

    public static object Scalar(string sql, params SqlParameter[] ps)
    {
        using var c = Open();
        using var cmd = new SqlCommand(sql, c);
        if (ps != null) cmd.Parameters.AddRange(ps);
        return cmd.ExecuteScalar();
    }

    /// <summary>Transaction đang mở trên luồng hiện tại (dùng nội bộ cho InTransaction).</summary>
    private static class TxScope
    {
        [ThreadStatic] private static SqlTransaction _tx;
        [ThreadStatic] private static SqlConnection _conn;

        public static SqlTransaction Current => _tx;
        public static SqlConnection Connection => _conn;

        public static IDisposable Enter(SqlTransaction tx)
        {
            var prevTx = _tx;
            var prevConn = _conn;
            _tx = tx;
            _conn = tx.Connection;
            return new Restore(prevTx, prevConn);
        }

        private sealed class Restore : IDisposable
        {
            private readonly SqlTransaction _pt;
            private readonly SqlConnection _pc;
            public Restore(SqlTransaction pt, SqlConnection pc) { _pt = pt; _pc = pc; }
            public void Dispose() { _tx = _pt; _conn = _pc; }
        }
    }
    public static SqlParameter P(string name, object value) => new SqlParameter(name, value ?? DBNull.Value);

    /// <summary>Chạy một khối công việc trong giao dịch, tự commit/rollback.</summary>
    public static T InTransaction<T>(Func<SqlConnection, SqlTransaction, T> action)
    {
        using var c = Open();
        using var tx = c.BeginTransaction();
        // Gắn transaction vào luồng hiện tại: mọi SqlCommand tạo trong block này
        // sẽ tự nhận tx (xem EnsureTx), tránh lỗi "requires the command to have a transaction".
        using (TxScope.Enter(tx))
        {
            try
            {
                var r = action(c, tx);
                tx.Commit();
                return r;
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }
    }

    /// <summary>Gắn transaction đang mở vào command nếu command chưa có.</summary>
    public static SqlCommand WithTx(SqlCommand cmd)
    {
        if (cmd.Transaction == null && TxScope.Current != null && cmd.Connection == TxScope.Connection)
            cmd.Transaction = TxScope.Current;
        return cmd;
    }

    /// <summary>Ánh xạ DataTable → danh sách đối tượng (theo tên cột, bỏ dấu gạch dưới).</summary>
    public static List<T> Map<T>(DataTable dt) where T : new()
    {
        var list = new List<T>();
        var props = typeof(T).GetProperties().Where(p => p.CanWrite).ToList();
        var cols = dt.Columns.Cast<DataColumn>().ToList();
        foreach (DataRow row in dt.Rows)
        {
            var obj = new T();
            foreach (var p in props)
            {
                var col = cols.FirstOrDefault(c =>
                    string.Equals(c.ColumnName.Replace("_", ""), p.Name, StringComparison.OrdinalIgnoreCase));
                if (col == null) continue;
                var val = row[col.ColumnName];
                if (val == DBNull.Value) continue;
                var target = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                try { p.SetValue(obj, Convert.ChangeType(val, target)); }
                catch { }
            }
            list.Add(obj);
        }
        return list;
    }

    public static T MapOne<T>(DataTable dt) where T : class, new()
        => dt.Rows.Count == 0 ? null : Map<T>(dt)[0];
}
