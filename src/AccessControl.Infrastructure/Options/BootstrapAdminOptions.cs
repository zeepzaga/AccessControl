namespace AccessControl.Infrastructure.Options;

public class BootstrapAdminOptions
{
    public const string SectionName = "Auth:BootstrapAdmin";

    public string Email { get; set; } = "admin@accesscontrol.local";
    public string Password { get; set; } = "AccessControl123!";
    public string FullName { get; set; } = "System Administrator";
}
