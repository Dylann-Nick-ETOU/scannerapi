using ApiSecurityScanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiSecurityScanner.Infrastructure.Persistence;

public class ApiSecurityScannerDbContext(DbContextOptions<ApiSecurityScannerDbContext> options) : DbContext(options)
{
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<SecurityIssue> SecurityIssues => Set<SecurityIssue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApiSecurityScannerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
