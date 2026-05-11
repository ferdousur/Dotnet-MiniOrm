using Npgsql;

namespace MiniOrm.Data;

public class DbContext : IDisposable
{
    private readonly string _connectionString; 
    private readonly NpgsqlConnection _connection; 
    
    public DbContext(string connectionString)
    {
        _connectionString=connectionString;
        _connection=new NpgsqlConnection(_connectionString); 
        _connection.Open(); 
    }

    public NpgsqlConnection connection()
    {
        return _connection; 
    }

    public void Dispose()
    {
        _connection.Dispose(); 
    }
}

