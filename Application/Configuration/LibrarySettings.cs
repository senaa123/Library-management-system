namespace LibraryM.Application.Configuration;

public sealed class LibrarySettings
{
    public const string SectionName = "Library";

    public int LoanPeriodDays { get; set; } = 14;

    public int MaxConcurrentLoans { get; set; } = 5;

    public int ReservationHoldDays { get; set; } = 5;

    public int RenewalDays { get; set; } = 7;

    public int MaxRenewals { get; set; } = 2;

    public decimal FinePerDay { get; set; } = 1.50m;
}
