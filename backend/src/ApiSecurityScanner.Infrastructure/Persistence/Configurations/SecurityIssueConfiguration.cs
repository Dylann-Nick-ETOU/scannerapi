using ApiSecurityScanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiSecurityScanner.Infrastructure.Persistence.Configurations;

public class SecurityIssueConfiguration : IEntityTypeConfiguration<SecurityIssue>
{
    public void Configure(EntityTypeBuilder<SecurityIssue> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RuleCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Endpoint).HasMaxLength(400).IsRequired();
        builder.Property(x => x.OpenApiLocation).HasMaxLength(1200).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.OwaspCategory).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OwaspTop10Id).HasMaxLength(20).IsRequired();
        builder.Property(x => x.OwaspTop10Version).HasMaxLength(10).IsRequired();
        builder.Property(x => x.OwaspTop10Title).HasMaxLength(200).IsRequired();
    }
}
