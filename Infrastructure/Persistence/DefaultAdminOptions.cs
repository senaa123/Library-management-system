namespace LibraryM.Infrastructure.Persistence;

public sealed class DefaultAdminOptions
{
    public const string SectionName = "DefaultAdmin";

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = "Admin@123";

    public string FullName { get; set; } = "System Administrator";

    public string Email { get; set; } = "admin@library.local";

    public string PhoneNumber { get; set; } = string.Empty;
}
