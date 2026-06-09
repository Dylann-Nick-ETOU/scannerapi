using ApiSecurityScanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiSecurityScanner.Infrastructure.Persistence.Configurations;

public class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AdminUsername).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ActionType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TargetUsername).HasMaxLength(100);
        builder.Property(x => x.Details).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.TargetUsername, x.CreatedAt });
    }
}
