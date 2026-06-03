using ApiSecurityScanner.Application.Rules;
using ApiSecurityScanner.Application.Services;
using ApiSecurityScanner.Application.UseCases;
using ApiSecurityScanner.Application.Validators;
using ApiSecurityScanner.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ApiSecurityScanner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISecurityRule, MissingAuthenticationRule>();
        services.AddScoped<ISecurityRule, SensitiveEndpointRule>();
        services.AddScoped<ISecurityRule, SensitiveDataExposureRule>();
        services.AddScoped<ISecurityRule, WeakInputValidationRule>();
        services.AddScoped<ISecurityRule, InsecureServerUrlRule>();

        services.AddScoped<SecurityRuleEngine>();
        services.AddScoped<ScanScoringService>();
        services.AddScoped<ScanOpenApiUseCase>();
        services.AddScoped<ScanOpenApiFileUseCase>();
        services.AddScoped<GetAllScansUseCase>();
        services.AddScoped<GetScanByIdUseCase>();
        services.AddScoped<DeleteScanUseCase>();
        services.AddScoped<IValidator<DTOs.ScanRequestDto>, ScanRequestDtoValidator>();
        return services;
    }
}
