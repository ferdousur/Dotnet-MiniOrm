
using Npgsql;

namespace MiniOrm.Data; 

public class DbSet<T> where T: new()
{
    private readonly NpgsqlConnection _conn; 
    private readonly string _tableName; 

    public DbSet(DbContext context, string tableName)
    {
        _conn=context.connection(); 
        _tableName=tableName; 
    }

    public void Add(T entity)
    {
        // Shob property niye aschi, kintu "Id" bad diye (karon DB auto-generate korbe)
        var properties=typeof(T).GetProperties().Where(p=> p.Name.ToLower() != "id").ToList();

         // Column names: "name, email, age"
        var columns= string.Join(", ", properties.Select(p=> p.Name.ToLower())); 

        // Parameter names: "@name, @email, @age"
        var values= string.Join(", ", properties.Select(p=> "@"+ p.Name.ToLower())); 

        //sql query
        var sql=$"INSERT INTO {_tableName} ({columns})  VALUES  ({values})"; 

        // Command create kore parameter set kora
        using var cmd= new NpgsqlCommand(sql, _conn); 

        foreach(var prop in properties)
        {
            // Object theke value niye aschi
            var value= prop.GetValue(entity);
            cmd.Parameters.AddWithValue("@" + prop.Name.ToLower(), value ?? DBNull.Value);  
        }

        cmd.ExecuteNonQuery();
        Console.WriteLine($"Added to {_tableName}");
    }


    public  List<T> GetAll()
    {
        // SELECT * FROM users
        var sql=$"SELECT * FROM  {_tableName}"; 
        using var cmd= new NpgsqlCommand(sql, _conn); 
        // Data read korar jonno reader
        using var reader= cmd.ExecuteReader();

        var results= new List<T>(); 

        // Prottek row er jonno loop
        while(reader.Read())
        {
            // New object create (like new User())
            var obj = new T(); 

            // Prottek property te database er value assign kora
            foreach( var prop in typeof(T).GetProperties())
            {
                // Column name dhore value niye aschi
                var value= reader[prop.Name.ToLower()]; 
                if(value != DBNull.Value)
                {
                     // Object er property te value set
                    prop.SetValue(obj, value); 
                }
            }
            results.Add(obj); 
        }
        return results; 
    }

    public T? Find(int id)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE id = @id";  

        using var cmd = new NpgsqlCommand(sql, _conn);
        cmd.Parameters.AddWithValue("@id", id);  

        using var reader = cmd.ExecuteReader();

        // Data na paile null return
        if (!reader.Read()) return default;  

        var obj = new T();

        foreach (var prop in typeof(T).GetProperties())
        {
            var value = reader[prop.Name.ToLower()];
            if (value != DBNull.Value)
                prop.SetValue(obj, value);
        }

        // Single object return (like User)
        return obj;  
    }

    public void Update(T entity)
    {
        // Id bad diye baki shob property
        var props = typeof(T).GetProperties()
            .Where(p => p.Name != "Id")
            .ToList();

        // SET part: "name = @Name, email = @Email"
        var setPart = string.Join(", ", props.Select(p => $"{p.Name.ToLower()} = @{p.Name}"));

        // Full SQL: UPDATE users SET name = @Name, email = @Email WHERE id = @Id
        var sql = $"UPDATE {_tableName} SET {setPart} WHERE id = @Id";

        using var cmd = new NpgsqlCommand(sql, _conn);

        // Baki property gular parameter set
        foreach (var prop in props)
        {
            var value = prop.GetValue(entity);
            cmd.Parameters.AddWithValue("@" + prop.Name, value ?? DBNull.Value);
        }

        // Id parameter alada vabe set (karon WHERE clause e lage)
        var idValue = typeof(T).GetProperty("Id")!.GetValue(entity)!;
        cmd.Parameters.AddWithValue("@Id", idValue);

        cmd.ExecuteNonQuery();
        Console.WriteLine($"Updated in {_tableName}");
    }


    public void Delete(int id)
    {
        var sql = $"DELETE FROM {_tableName} WHERE id = @id";  

        using var cmd = new NpgsqlCommand(sql, _conn);
        cmd.Parameters.AddWithValue("@id", id);

        // Execute kore delete
        cmd.ExecuteNonQuery();  
        Console.WriteLine($"Deleted id={id} from {_tableName}");
    }
}



















