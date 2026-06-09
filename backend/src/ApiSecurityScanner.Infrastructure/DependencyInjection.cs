using ApiSecurityScanner.Application;
using ApiSecurityScanner.Domain.Interfaces;
using ApiSecurityScanner.Infrastructure.OpenApi;
using ApiSecurityScanner.Infrastructure.Persistence;
using ApiSecurityScanner.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace ApiSecurityScanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApiSecurityScannerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpClient<IOpenApiDocumentLoader, OpenApiDocumentLoader>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ApiSecurityScanner/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
        services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
        services.AddScoped<IScanRepository, ScanRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
