namespace LibraryM.Infrastructure.Persistence;

public sealed class DefaultAdminOptions
{
    public const string SectionName = "DefaultAdmin";
    public const string PasswordPlaceholder = "replace-this-in-api-dotenv-with-a-local-admin-password";

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = PasswordPlaceholder;

    public string FullName { get; set; } = "System Administrator";

    public string Email { get; set; } = "admin@library.local";

    public string PhoneNumber { get; set; } = string.Empty;
}
