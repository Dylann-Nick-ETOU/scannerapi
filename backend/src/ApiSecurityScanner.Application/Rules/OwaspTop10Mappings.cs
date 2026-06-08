namespace ApiSecurityScanner.Application.Rules;

internal sealed record OwaspTop10Mapping(string Id, string Version, string Title);

internal static class OwaspTop10Mappings
{
    public static readonly OwaspTop10Mapping BrokenAuthentication2023 = new("API2", "2023", "Broken Authentication");
    public static readonly OwaspTop10Mapping BrokenFunctionLevelAuthorization2023 = new("API5", "2023", "Broken Function Level Authorization");
    public static readonly OwaspTop10Mapping SecurityMisconfiguration2023 = new("API8", "2023", "Security Misconfiguration");
    public static readonly OwaspTop10Mapping ImproperInventoryManagement2023 = new("API9", "2023", "Improper Inventory Management");
    public static readonly OwaspTop10Mapping ExcessiveDataExposure2019 = new("API3", "2019", "Excessive Data Exposure");
    public static readonly OwaspTop10Mapping MassAssignment2019 = new("API6", "2019", "Mass Assignment");
}
