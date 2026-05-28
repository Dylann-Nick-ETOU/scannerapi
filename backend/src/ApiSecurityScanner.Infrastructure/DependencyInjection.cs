using ApiSecurityScanner.Application;
using ApiSecurityScanner.Domain.Interfaces;
using ApiSecurityScanner.Infrastructure.OpenApi;
using ApiSecurityScanner.Infrastructure.Persistence;
using ApiSecurityScanner.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiSecurityScanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApiSecurityScannerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpClient<IOpenApiDocumentLoader, OpenApiDocumentLoader>();
        services.AddScoped<IScanRepository, ScanRepository>();
        return services;
    }
}
