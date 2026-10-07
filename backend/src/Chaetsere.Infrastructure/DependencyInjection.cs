using Chaetsere.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Chaetsere.Infrastructure;

public static class DependencyInjection
{
    /// <summary>In the API's Program.cs: builder.Services.AddPersistence(builder.Configuration.GetConnectionString("Db")!);</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => Configure(options, connectionString));
        return services;
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention();
}

/// <summary>
/// Lets `dotnet ef` run before the API project exists:
///   export CHAETSERE_DB="Host=localhost;Database=chaetsere;Username=postgres;Password=postgres"
///   dotnet ef migrations add Initial -p src/Chaetsere.Infrastructure -s src/Chaetsere.Infrastructure -o Persistence/Migrations
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("CHAETSERE_DB")
                 ?? "Host=localhost;Port=5432;Database=chaetsere;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<AppDbContext>();
        DependencyInjection.Configure(options, cs);
        return new AppDbContext(options.Options);
    }
}
