namespace LibraryM.Application.Configuration;

public sealed class LibrarySettings
{
    public const string SectionName = "Library";

    public int LoanPeriodDays { get; set; } = 14;

    public int MaxConcurrentLoans { get; set; } = 5;

    public int ReservationHoldDays { get; set; } = 5;

    public int RenewalDays { get; set; } = 7;

    public int MaxRenewals { get; set; } = 2;

    public decimal BaseFinePerDay { get; set; } = 0.50m;

    public int FineEscalationAfterDays { get; set; } = 10;

    public decimal EscalatedFinePerDay { get; set; } = 1.00m;

    public decimal ReservationNoShowFine { get; set; } = 0.25m;

    public decimal DamagedBookFine { get; set; } = 0.50m;

    public decimal LostBookFine { get; set; } = 10.00m;

    public decimal MissingPagesFine { get; set; } = 1.00m;

    public decimal LimitedCirculationFineThreshold { get; set; } = 2.00m;

    public decimal BlockingFineThreshold { get; set; } = 5.00m;

    public int LimitedCirculationItems { get; set; } = 2;
}
