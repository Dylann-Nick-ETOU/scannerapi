namespace ApiSecurityScanner.Domain.Entities;

public class ScanSummary
{
    public int TotalIssues { get; set; }
    public int Critical { get; set; }
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
}
