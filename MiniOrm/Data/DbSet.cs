using MiniOrm.Data;
using Npgsql;

namespace MiniOrm.Data;

public class DbSet<T> where T : new()
{
    private readonly NpgsqlConnection _conn;
    private readonly string _table;

    public DbSet(DbContext context, string tableName)
    {
        _conn = context.connection();
        _table = tableName;
    }

    // insert record without Id (handled by DB identity)
    public void Add(T entity)
    {
        var props = typeof(T).GetProperties()
            .Where(p => p.Name.ToLower() != "id")
            .ToList();

        var columns = string.Join(", ",
            props.Select(p => p.Name.ToLower()));

        var values = string.Join(", ",
            props.Select(p => "@" + p.Name.ToLower()));

        var sql = $"INSERT INTO {_table} ({columns}) VALUES ({values})";

        using var cmd = new NpgsqlCommand(sql, _conn);

        foreach (var prop in props)
        {
            var value = prop.GetValue(entity);

            cmd.Parameters.AddWithValue(
                "@" + prop.Name.ToLower(),
                value ?? DBNull.Value);
        }

        cmd.ExecuteNonQuery();

        Console.WriteLine($"Added to {_table}");
    }

    // get all records from table
    public List<T> GetAll()
    {
        var sql = $"SELECT * FROM {_table}";

        using var cmd = new NpgsqlCommand(sql, _conn);
        using var reader = cmd.ExecuteReader();

        var results = new List<T>();

        while (reader.Read())
        {
            var obj = new T();

            foreach (var prop in typeof(T).GetProperties())
            {
                var value = reader[prop.Name.ToLower()];

                if (value != DBNull.Value)
                    prop.SetValue(obj, value);
            }

            results.Add(obj);
        }

        return results;
    }

    // find record by id
    public T? Find(int id)
    {
        var sql = $"SELECT * FROM {_table} WHERE id = @id";

        using var cmd = new NpgsqlCommand(sql, _conn);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return default;

        var obj = new T();

        foreach (var prop in typeof(T).GetProperties())
        {
            var value = reader[prop.Name.ToLower()];

            if (value != DBNull.Value)
                prop.SetValue(obj, value);
        }

        return obj;
    }

    // update record by id
    public void Update(T entity)
    {
        var props = typeof(T).GetProperties()
            .Where(p => p.Name != "Id")
            .ToList();

        var setPart = string.Join(", ",
            props.Select(p => $"{p.Name.ToLower()} = @{p.Name}"));

        var sql = $"UPDATE {_table} SET {setPart} WHERE id = @Id";

        using var cmd = new NpgsqlCommand(sql, _conn);

        foreach (var prop in props)
        {
            var value = prop.GetValue(entity);
            cmd.Parameters.AddWithValue("@" + prop.Name, value ?? DBNull.Value);
        }

        var idValue = typeof(T).GetProperty("Id")!.GetValue(entity)!;
        cmd.Parameters.AddWithValue("@Id", idValue);

        cmd.ExecuteNonQuery();

        Console.WriteLine($"Updated in {_table}");
    }

    // delete record by id
    public void Delete(int id)
    {
        var sql = $"DELETE FROM {_table} WHERE id = @id";

        using var cmd = new NpgsqlCommand(sql, _conn);
        cmd.Parameters.AddWithValue("@id", id);

        cmd.ExecuteNonQuery();

        Console.WriteLine($"Deleted id={id} from {_table}");
    }
}