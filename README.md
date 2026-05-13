# MiniOrm - Custom ORM Framework

A lightweight, educational ORM built with .NET 10.0 & PostgreSQL.

##  Features

-  Attribute-based mapping: [Table], [Column], [PrimaryKey]
-  [DbContext] & [DbSet<T>] for clean query abstraction
-  Auto type mapping: C# → PostgreSQL via [TypeMapper]
-  CLI-based migration system with version control
-  Built on Npgsql for reliable PostgreSQL connectivity

##  Project Structure

FinalAssignmentMiniOrm/
├── MiniOrm/                  # Core ORM Library
│   ├── Attributes/           # Mapping attributes
│   ├── Data/                 # DbContext, DbSet, TypeMapper
│   ├── Models/               # Sample entities (Product, Order)
│   └── Program.cs            # Demo entry point
│
├── MiniOrm.Migrations/       # Migration CLI Tool
│   ├── MigrationRunner.cs    # Add/Apply/List/Rollback logic
│   └── Program.cs            # CLI argument handler
│
├── MiniOrm.slnx              # Solution file
└── README.md                 # This file

##  Quick Start

# Restore & build
dotnet restore && dotnet build

# Run main demo
dotnet run --project MiniOrm

# Run migration tool
dotnet run --project MiniOrm.Migrations

##  Migration Commands

| Command    | Description               | Example                                                               |
|------------|---------------------------|-----------------------------------------------------------------------|
| `add`      | Create new migration      | `dotnet run --project MiniOrm.Migrations -- add InitialTableCreation` |
| `apply`    | Apply pending migrations  | `dotnet run --project MiniOrm.Migrations -- apply`                    |
| `list`     | Show migration history    | `dotnet run --project MiniOrm.Migrations -- list`                     |
| `rollback` | Revert last migration     | `dotnet run --project MiniOrm.Migrations -- rollback`                 |
