# Database Configuration

This application supports both SQL Server and SQLite databases. You can switch between them using configuration settings.

## Configuration

The database provider is configured in the `appsettings.json` or `appsettings.Development.json` files:

```json
{
  "DatabaseSettings": {
    "Provider": "SQLite",  // or "SqlServer"
    "SqliteDataDirectory": "Data"
  },
  "ConnectionStrings": {
    "SqlServerConnection": "Server=127.0.0.1,1433;Database=elective_2;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False",
    "SqliteConnection": "Data Source=Data/elective_gradesheet.db"
  }
}
```

## Database Providers

### SQL Server
- **Provider**: `"SqlServer"`
- **Connection String**: `SqlServerConnection`
- **Features**: Full Entity Framework migrations support
- **Best for**: Production environments, complex queries, multiple concurrent users

### SQLite  
- **Provider**: `"SQLite"`
- **Connection String**: `SqliteConnection`
- **Features**: File-based database, automatic database creation
- **Best for**: Development, single-user scenarios, portable deployments

## Switching Between Providers

1. **To use SQLite** (Development):
   - Set `"Provider": "SQLite"` in `appsettings.Development.json`
   - The database file will be created automatically in the `Data` directory

2. **To use SQL Server** (Production):
   - Set `"Provider": "SqlServer"` in `appsettings.json`
   - Ensure SQL Server is running and the connection string is correct
   - Run migrations if needed

## Service Implementations

- **SQL Server**: Uses `GradeDbService` with `ApplicationDbContext`
- **SQLite**: Uses `GradeDbLiteService` with `ApplicationDbLiteContext`

Both implementations provide identical functionality through the `IGradeService` interface.

## Database Initialization

- **SQL Server**: Uses Entity Framework migrations (`Database.MigrateAsync()`)
- **SQLite**: Uses automatic database creation (`Database.EnsureCreatedAsync()`)

## File Structure

```
elective-2-gradesheet/
├── Data/
│   ├── ApplicationDbContext.cs          # SQL Server context
│   ├── ApplicationDbLiteContext.cs      # SQLite context
│   └── elective_gradesheet.db          # SQLite database file (auto-created)
├── Services/
│   ├── IGradeService.cs                # Interface
│   ├── GradeDbService.cs              # SQL Server implementation
│   └── GradeDbLiteService.cs          # SQLite implementation
└── Configuration/
    └── DatabaseConfiguration.cs       # Configuration helper
```

## Important Notes

1. Both database providers use the same data models and provide identical functionality
2. SQLite databases are created automatically when the application starts
3. SQL Server requires manual database setup and connection configuration
4. The `DatabaseSettings:SqliteDataDirectory` setting controls where SQLite files are stored
5. Connection strings support environment-specific overrides through `appsettings.{Environment}.json`
