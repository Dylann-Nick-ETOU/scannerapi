using ApiSecurityScanner.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiSecurityScanner.Tests.Integration;

public class ApiSecurityScannerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:SeedUsers:0:Username"] = "admin",
                ["Authentication:SeedUsers:0:PasswordHash"] = "AQAAAAIAAYagAAAAEEhwfBl6N3zNjZ5yjmS45k2Llnt0qUfJxFKZ7JnKFFV7lacRM0LhCpXmIx6WoAQJZA==",
                ["Authentication:SeedUsers:0:Role"] = "Admin",
                ["Authentication:SeedUsers:1:Username"] = "auditor",
                ["Authentication:SeedUsers:1:PasswordHash"] = "AQAAAAIAAYagAAAAELjZ0pm7oMsDdOQKv66LZUKKAGaZnTZLEUlmyHeoiKvyK0nAkZRomsCaBzX4fU8czg==",
                ["Authentication:SeedUsers:1:Role"] = "User"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApiSecurityScannerDbContext>>();
            services.RemoveAll<ApiSecurityScannerDbContext>();

            services.AddSingleton(_connection);
            services.AddDbContext<ApiSecurityScannerDbContext>((serviceProvider, options) =>
                options.UseSqlite(serviceProvider.GetRequiredService<SqliteConnection>()));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiSecurityScannerDbContext>();
        db.SecurityIssues.RemoveRange(db.SecurityIssues);
        db.Scans.RemoveRange(db.Scans);
        db.AppUsers.RemoveRange(db.AppUsers);
        await db.SaveChangesAsync();
    }
}
