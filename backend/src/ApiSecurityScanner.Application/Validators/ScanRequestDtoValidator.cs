using ApiSecurityScanner.Application.DTOs;
using FluentValidation;

namespace ApiSecurityScanner.Application.Validators;

public class ScanRequestDtoValidator : AbstractValidator<ScanRequestDto>
{
    public ScanRequestDtoValidator()
    {
        RuleFor(x => x.OpenApiUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("openApiUrl must be a valid HTTP/HTTPS URL.");
    }
}
