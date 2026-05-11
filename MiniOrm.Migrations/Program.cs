using Microsoft.Extensions.DependencyInjection;
using MiniOrm.Data;
using MiniOrm.Migrations;
using MiniOrm.Models;

var connectionString = "Host=localhost:5433;Database=miniorm_db;Username=postgres;Password=password";

var services = new ServiceCollection();

services.AddSingleton(new DbContext(connectionString));

services.AddSingleton<MigrationTableCreator>();

services.AddSingleton<EntityMedatada<Products>>();
services.AddSingleton<EntityMedatada<Order>>();

services.AddSingleton<MigrationRunner<Products>>();
services.AddSingleton<MigrationRunner<Order>>();

var provider = services.BuildServiceProvider();

var migrationTableCreator = provider.GetRequiredService<MigrationTableCreator>();

var productRunner = provider.GetRequiredService<MigrationRunner<Products>>();
var orderRunner = provider.GetRequiredService<MigrationRunner<Order>>();

migrationTableCreator.CreateTable();

if (args.Length == 0)
{
    Console.WriteLine("Command Needed");
    return;
}

var command = args[0];

if (command == "add")
{
    var migrationName = args[1];

    productRunner.AddCommand(migrationName);
    orderRunner.AddCommand(migrationName);
}

if (command == "apply")
{
    productRunner.ApplyCommand();
    orderRunner.ApplyCommand();
}

if (command == "list")
{
    productRunner.ListCommand();
}

if (command == "rollback")
{
    productRunner.Rollback();
    orderRunner.Rollback();
}

// dotnet run -- add InitialTables
// dotnet run -- apply 
// dotnet run -- list
// dotnet run -- rollback