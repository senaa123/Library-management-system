namespace LibraryM.Application.Fines.Models;

public sealed record VerifiedFineCheckoutResult(
    string SessionId,
    decimal AmountPaid,
    string PaymentMethod,
    string ExternalReference,
    bool IsPaid);
