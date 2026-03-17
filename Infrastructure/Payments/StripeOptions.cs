namespace LibraryM.Infrastructure.Payments;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string PublishableKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string ClientBaseUrl { get; set; } = "http://localhost:5173";

    public string Currency { get; set; } = "usd";
}
