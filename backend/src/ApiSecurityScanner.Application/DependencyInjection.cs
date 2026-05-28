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
        services.AddScoped<SecurityRuleEngine>();
        services.AddScoped<ScanScoringService>();
        services.AddScoped<ScanOpenApiUseCase>();
        services.AddScoped<ScanOpenApiFileUseCase>();
        services.AddScoped<IValidator<DTOs.ScanRequestDto>, ScanRequestDtoValidator>();
        return services;
    }
}
