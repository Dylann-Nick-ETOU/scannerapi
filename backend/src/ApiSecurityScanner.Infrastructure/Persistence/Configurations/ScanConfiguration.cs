using ApiSecurityScanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiSecurityScanner.Infrastructure.Persistence.Configurations;

public class ScanConfiguration : IEntityTypeConfiguration<Scan>
{
    public void Configure(EntityTypeBuilder<Scan> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OwnerId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TargetName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OpenApiUrl).HasMaxLength(2048);
        builder.Property(x => x.Score).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.OwnerId, x.CreatedAt });
        builder.HasMany(x => x.SecurityIssues).WithOne(x => x.Scan).HasForeignKey(x => x.ScanId).OnDelete(DeleteBehavior.Cascade);
    }
}
