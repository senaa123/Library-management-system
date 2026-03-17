namespace LibraryM.Application.Fines.Models;

public sealed record CreateFineCheckoutSessionResult(
    string SessionId,
    string CheckoutUrl,
    decimal Amount);
