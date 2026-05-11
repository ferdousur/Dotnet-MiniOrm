using System.Data;
using System.Security.Cryptography.X509Certificates;
using MiniOrm.Data;
using Npgsql;

namespace MiniOrm.Migrations;




//store all the migraitons
public class Migration
{
    public string Name {get;set;}
    public string UpSql {get;set;}
    public string DownSql {get;set;}
}


//Migraiton Table Creator class; 
public class MigrationTableCreator
{
    private readonly DbContext _context;

    public MigrationTableCreator(DbContext context)
    {
        _context=context; 
    } 

    public void CreateTable()
    {
        OpenConnection();
        var sql = @"
            CREATE TABLE IF NOT EXISTS _migrations
            (
                id SERIAL PRIMARY KEY,
                name TEXT,
                upsql TEXT,
                downsql TEXT,
                applied_at TIMESTAMP NULL
            );
        ";
        using var cmd= new NpgsqlCommand(sql, _context.connection()); 

        cmd.ExecuteNonQuery(); 
        Console.WriteLine("_migraiton Table Ready"); 
    }

    public void OpenConnection()
    {
        if(_context.connection().State !=ConnectionState.Open)
        {
            _context.connection().Open(); 
        }
    }

}


public class MigrationRunner<T>
{
    private readonly DbContext _context; 
    private readonly EntityMedatada<T> _meta; 
    public MigrationRunner(DbContext context, EntityMedatada<T> meta )
    {
        _context=context;
        _meta=meta;
    }

    //connectionstate check if already connection then no need to new connection open
    public void OpenConnection()
    {
        if(_context.connection().State !=ConnectionState.Open)
        {
            _context.connection().Open(); 
        }
    }

    public void AddCommand(string migrationName)
    {
        OpenConnection(); 
        var tableName= _meta.TableName; 
        var columns = new List<string>();
        foreach(var col in _meta.Columns)
        {
            var pgType=TypeMapper.ToPostgresType(col.PropertyType); 
            if(col.IsPrimaryKey)
            {
                columns.Add($"{col.ColumnName} {pgType} GENERATED ALWAYS AS IDENTITY PRIMARY KEY"); 
            }
            else
            {
                columns.Add($"{col.ColumnName} {pgType}");
            }
        } 

        var upsql=$@"CREATE TABLE IF NOT EXISTS  {tableName} ({string.Join(",\n", columns)})"; 

        var downsql=$@"DROP TABLE IF EXISTS {tableName}";

        //now update _migration table 
        var insertSql= @"INSERT INTO _migrations (name, upsql, downsql) VALUES (@name, @upsql, @downsql)"; 

        using var cmd= new NpgsqlCommand(insertSql, _context.connection()); 
        cmd.Parameters.AddWithValue("@name", migrationName); 
        cmd.Parameters.AddWithValue("@upsql", upsql); 
        cmd.Parameters.AddWithValue("@downsql", downsql); 
        cmd.ExecuteNonQuery(); 

        Console.WriteLine($"Migration Added: {migrationName}");
    }

    public void ApplyCommand()
    {
       try
        {
            OpenConnection(); 
            var sql= "SELECT name, upsql FROM _migrations WHERE applied_at IS NULL"; 
            var migrations= new List<Migration>(); 
            using var cmd = new NpgsqlCommand(sql, _context.connection()); 
            using (var reader = cmd.ExecuteReader())
            {
                while(reader.Read())
                {
                    migrations.Add(new Migration
                    {
                        Name = reader.GetString(0),
                        UpSql = reader.GetString(1)
                    });
                }
            }
            if(migrations.Count ==0)
            {
                Console.WriteLine("All migrations already applied.");
                return;

            }

            foreach(var migration in migrations)
            {
                Console.WriteLine($"Applying {migration.Name} in Database...."); 


                using var transaction= _context.connection().BeginTransaction(); 

                try
                {   
                    using var applyCmd=new NpgsqlCommand(migration.UpSql, _context.connection(), transaction); 
                    var rows = applyCmd.ExecuteNonQuery(); 
                    Console.WriteLine($"Schema changed. Rows affected: {rows}");

                    using var updatecmd= new NpgsqlCommand("UPDATE _migrations SET applied_at =NOW() WHERE @name=name", _context.connection(), transaction);
                    updatecmd.Parameters.AddWithValue("@name", migration.Name);
                    updatecmd.ExecuteNonQuery(); 
                    transaction.Commit(); 
                    Console.WriteLine($"Done: {migration.Name}");
                }

                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        catch (NpgsqlException ex)
        {
            Console.WriteLine($"Database Error: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

    public void ListCommand()
    {
        try
        {
            OpenConnection(); 
          var sql = "SELECT name, applied_at FROM _migrations ORDER BY id";

            using var cmd= new NpgsqlCommand(sql, _context.connection()); 
            var reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                Console.WriteLine("No Migrations Found to roolback");
                return;
            }

            while(reader.Read())
            {
                var name=reader.GetString(0);
                var status = reader.IsDBNull(1) ? "Pending" : "Applied"; 
                Console.WriteLine($"{name} --> {status}");
            }
        }

        catch (NpgsqlException ex)
        {
            Console.WriteLine($"Database Error: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }
    public void Rollback()
    {
        try
        {
            OpenConnection(); 
            var sql= @"SELECT name, downsql FROM _migrations WHERE applied_at IS NOT NULL ORDER BY id DESC LIMIT 1";
            
            using var cmd= new NpgsqlCommand(sql, _context.connection()); 
            using var reader=cmd.ExecuteReader(); 

            Migration migration= null; 
            {

                if(reader.Read())
                {
                    migration = new Migration
                    {
                        Name=reader.GetString(0), 
                        DownSql=reader.GetString(1)
                    }; 
                }



            }

            if(migration==null)
            {
                Console.WriteLine("There is no migration to roolback"); 
                return;
            }

            Console.WriteLine($"Rolling Back: {migration.Name}");

            //clock reader conneciton 
            reader.Close();
            
            var transaction=_context.connection().BeginTransaction(); 

            try
            {
                using var roolbackSql= new NpgsqlCommand(migration.DownSql, _context.connection(), transaction); 
                var rows= roolbackSql.ExecuteNonQuery(); 
                Console.WriteLine($"Schema reverted. Rows affected: {rows}");

                // migration delete
                using var deleteCmd = new NpgsqlCommand(@"DELETE FROM _migrations WHERE @name = name;",_context.connection(), transaction);

                deleteCmd.Parameters.AddWithValue("@name",migration.Name);
                deleteCmd.ExecuteNonQuery();
                transaction.Commit(); 

                Console.WriteLine($"Rollback Done: {migration.Name}");
            }
        catch (Exception ex)
        {
            transaction.Rollback(); 
            Console.WriteLine($"Rollback failed: {ex.Message}");
            throw;
        }
            
        }
        catch (NpgsqlException ex)
        {
            Console.WriteLine($"Database Error: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }
  
}

















































// using Npgsql;
// using MiniOrm.Data;
// using MiniOrm.Models;
// using System.Text.RegularExpressions;


// // This class creates the _migrations table in the database if it does not exist
// public class MigrationTableCreation
// {
//     // Store the DbContext so we can use it to run database commands
//     private readonly DbContext _connection;

//     // Constructor: receives a DbContext and saves it for later use
//     public MigrationTableCreation(DbContext connection)
//     {
//         _connection = connection;
//     }

//     // This method creates the _migrations table with all required columns
//     public void CreateMigrationTable()
//     {
//         try
//         {
//             // Make sure the database connection is open before running commands
//             if (_connection.connection().State != System.Data.ConnectionState.Open)
//             {
//                 _connection.connection().Open();
//             }

//             // Create a new SQL command to create the _migrations table
//             // IF NOT EXISTS means it will not error if the table already exists
//             using var sqlCmd = new NpgsqlCommand(@"
//                 CREATE TABLE IF NOT EXISTS _migrations(
//                     id          SERIAL PRIMARY KEY, 
//                     name        TEXT NOT NULL, 
//                     applied_at  TIMESTAMP NULL,
//                     upsql       TEXT,
//                     downsql     TEXT
//                 );", _connection.connection());

//             // Execute the command to create the table
//             sqlCmd.ExecuteNonQuery();

//             // Print success message to console
//             // Console.WriteLine("Migration table created successfully");
//         }
//         catch (NpgsqlException ex)
//         {
//             // If there is a database error, print the error message
//             Console.WriteLine("Database error: " + ex.Message);
//             throw;
//         }
//         catch (Exception ex)
//         {
//             // If there is any other error, print the error message
//             Console.WriteLine("Error: " + ex.Message);
//             throw;
//         }
//     }
// }

// public class MigrationRunner
// {
//     private readonly EntityMedatada<Products> _meta;
//     private readonly DbContext _connection;
    
//     public string Command;
//     public string? Argument;
//     public long Number = 0;

//     public MigrationRunner(
//         DbContext connection,
//         EntityMedatada<Products> meta,
//         string[]? args = null)
//     {
//         // Throw exception if required dependencies are null
//         _connection = connection ?? throw new ArgumentNullException(nameof(connection));
//         _meta = meta ?? throw new ArgumentNullException(nameof(meta));

//         // Set default values
//         Command = "help";
//         Argument = null;

//         // Parse arguments if provided
//         if (args != null && args.Length > 0)
//         {
//             Command = args.FirstOrDefault() ?? "help";
//             Argument = args.ElementAtOrDefault(1);
//         }
//     }

//     public void AddCommand()
//     {
//         if (string.IsNullOrEmpty(Argument))
//         {
//             Console.WriteLine("Please give an argument. Example: dotnet run -- add CreateProduct");
//             return;
//         }

//         try
//         {
//             if (_connection.connection().State != System.Data.ConnectionState.Open)
//             {
//                 _connection.connection().Open();
//             }

//             // Fix: Handle possible null return from ExecuteScalar
//             using var countCmd = new NpgsqlCommand("SELECT COUNT(*) FROM _migrations", _connection.connection());
//             var countResult = countCmd.ExecuteScalar();
//             Number = (countResult != null) ? (long)countResult + 1 : 1;

//             var migrationName = $"M{Number:D3}_{Argument}";
//             var columns = new List<string>();

//             // Fix: Check if _meta.Columns is null before iterating
//             if (_meta.Columns != null)
//             {
//                 foreach (var col in _meta.Columns)
//                 {
//                     var pgType = TypeMapper.ToPostgresType(col.PropertyType);

//                     if (col.IsPrimaryKey)
//                     {
//                         columns.Add(
//                             $"{col.ColumnName} {pgType} GENERATED ALWAYS AS IDENTITY PRIMARY KEY"
//                         );
//                     }
//                     else
//                     {
//                         columns.Add($"{col.ColumnName} {pgType}");
//                     }
//                 }
//             }

//             // Fix: Check if TableName is null before using
//             var tableName = _meta.TableName ?? "products";
            
//             var upsql = "CREATE TABLE IF NOT EXISTS " + tableName + " ( \n" +
//                 string.Join(", \n", columns) +
//                 "\n);";
//             var downsql = "DROP TABLE IF EXISTS " + tableName + ";";

//             Console.WriteLine("Migration created: " + migrationName);

//             // Validate table name to prevent SQL injection
//             if (!string.IsNullOrEmpty(tableName) && Regex.IsMatch(tableName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
//             {
//                 using var saveCmd = new NpgsqlCommand(
//                     "INSERT INTO _migrations (name, upsql, downsql) VALUES (@name, @upsql, @downsql)",
//                     _connection.connection());

//                 saveCmd.Parameters.AddWithValue("@name", migrationName);
//                 saveCmd.Parameters.AddWithValue("@upsql", upsql);
//                 saveCmd.Parameters.AddWithValue("@downsql", downsql);

//                 saveCmd.ExecuteNonQuery();
//             }
//             else
//             {
//                 Console.WriteLine("Error: Invalid table name");
//             }
//         }
//         catch (NpgsqlException ex)
//         {
//             Console.WriteLine("Database error: " + ex.Message);
//             throw;
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine("Error: " + ex.Message);
//             throw;
//         }
//     }

//     public void ApplyCommand()
//     {
//         try
//         {
//             if (_connection.connection().State != System.Data.ConnectionState.Open)
//             {
//                 _connection.connection().Open();
//             }

//             var migrations = new List<(string name, string sql)>();

//             // Read pending migrations into a list first
//             // The curly braces create a new scope. When the block ends, 
//             // reader and pendingCmd are automatically disposed, freeing the connection.
//             {
//                 using var pendingCmd = new NpgsqlCommand(
//                     "SELECT name, upsql FROM _migrations WHERE upsql IS NOT NULL AND applied_at IS NULL",
//                     _connection.connection());

//                 using var reader = pendingCmd.ExecuteReader();
//                 while (reader.Read())
//                 {
//                     var name = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
//                     var sql = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    
//                     if (!string.IsNullOrEmpty(sql))
//                     {
//                         migrations.Add((name, sql));
//                     }
//                 }
//             } // Reader is fully closed here

//             if (migrations.Count == 0)
//             {
//                 Console.WriteLine("All migrations already applied.");
//                 return;
//             }

//             foreach (var migration in migrations)
//             {
//                 Console.WriteLine("Applying: " + migration.name);

//                 // Connection is now free, so transaction can be started safely
//                 using var transaction = _connection.connection().BeginTransaction();
//                 try
//                 {
//                     using var applySql = new NpgsqlCommand(migration.sql, _connection.connection(), transaction);
//                     applySql.ExecuteNonQuery();

//                     using var updateCmd = new NpgsqlCommand(
//                         "UPDATE _migrations SET applied_at = NOW() WHERE name = @name",
//                         _connection.connection(), transaction);
//                     updateCmd.Parameters.AddWithValue("@name", migration.name);
//                     updateCmd.ExecuteNonQuery();

//                     transaction.Commit();
//                     Console.WriteLine("Done: " + migration.name);
//                 }
//                 catch
//                 {
//                     transaction.Rollback();
//                     throw;
//                 }
//             }
//         }
//         catch (NpgsqlException ex)
//         {
//             Console.WriteLine("Database error: " + ex.Message);
//             throw;
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine("Error: " + ex.Message);
//             throw;
//         }
//     }

//     public void ListCommand()
//     {
//         try
//         {
//             if (_connection.connection().State != System.Data.ConnectionState.Open)
//             {
//                 _connection.connection().Open();
//             }

//             using var listCmd = new NpgsqlCommand(
//                 "SELECT name, applied_at FROM _migrations ORDER BY id",
//                 _connection.connection());

//             using var reader = listCmd.ExecuteReader();
//             Console.WriteLine("\n=== Migration Status ===");
//             while (reader.Read())
//             {
//                 var name = reader.IsDBNull(0) ? "unknown" : reader.GetString(0);
//                 var appliedAt = reader.IsDBNull(1) ? null : (DateTime?)reader.GetDateTime(1);
//                 var status = appliedAt == null ? "pending" : "applied";
//                 Console.WriteLine("  " + name + "  ->  " + status);
//             }
//         }
//         catch (NpgsqlException ex)
//         {
//             Console.WriteLine("Database error: " + ex.Message);
//             throw;
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine("Error: " + ex.Message);
//             throw;
//         }
//     }

//     public void RollBackCommand()
//     {
//         try
//         {
//             if (_connection.connection().State != System.Data.ConnectionState.Open)
//             {
//                 _connection.connection().Open();
//             }

//             var migrations = new List<(string name, string downsql)>();

//             // Read rollback data first, then close the reader
//             {
//                 using var pendingCmd = new NpgsqlCommand(
//                     @"SELECT name, downsql
//                     FROM _migrations
//                     WHERE downsql IS NOT NULL
//                     AND applied_at IS NOT NULL
//                     ORDER BY id DESC
//                     LIMIT 1",
//                     _connection.connection());

//                 using var reader = pendingCmd.ExecuteReader();
//                 while (reader.Read())
//                 {
//                     var name = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
//                     var downsql = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    
//                     if (!string.IsNullOrEmpty(downsql))
//                     {
//                         migrations.Add((name, downsql));
//                     }
//                 }
//             } // Reader is fully closed here

//             if (migrations.Count == 0)
//             {
//                 Console.WriteLine("No rollback migration found.");
//                 return;
//             }

//             foreach (var migration in migrations)
//             {
//                 Console.WriteLine("Rolling back: " + migration.name);

//                 using var transaction = _connection.connection().BeginTransaction();
//                 try
//                 {
//                     using var applyDownSql = new NpgsqlCommand(
//                         migration.downsql,
//                         _connection.connection(), transaction);
//                     applyDownSql.ExecuteNonQuery();

//                     using var deleteCmd = new NpgsqlCommand(
//                         @"DELETE FROM _migrations WHERE name = @name",
//                         _connection.connection(), transaction);
//                     deleteCmd.Parameters.AddWithValue("@name", migration.name);
//                     deleteCmd.ExecuteNonQuery();

//                     transaction.Commit();
//                     Console.WriteLine("Rollback done: " + migration.name);
//                 }
//                 catch
//                 {
//                     transaction.Rollback();
//                     throw;
//                 }
//             }
//         }
//         catch (NpgsqlException ex)
//         {
//             Console.WriteLine("Database error: " + ex.Message);
//             throw;
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine("Error: " + ex.Message);
//             throw;
//         }
//     }
// }