namespace ApiSecurityScanner.API.Authentication;

public class AuthOptions
{
    public List<ConfiguredUser> SeedUsers { get; set; } = [];
}

public class ConfiguredUser
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}
