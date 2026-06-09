using ApiSecurityScanner.Domain.Entities;

namespace ApiSecurityScanner.Application.DTOs;

public static class AdminAuditLogDtoMapper
{
    public static AdminAuditLogDto ToDto(this AdminAuditLog log) => new()
    {
        Id = log.Id,
        AdminUsername = log.AdminUsername,
        ActionType = log.ActionType,
        TargetUsername = log.TargetUsername,
        TargetScanId = log.TargetScanId,
        Details = log.Details,
        CreatedAt = log.CreatedAt
    };
}
