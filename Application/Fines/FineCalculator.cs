using LibraryM.Application.Configuration;
using LibraryM.Domain.Entities;

namespace LibraryM.Application.Fines;

public static class FineCalculator
{
    public static decimal CalculateAccruedFine(Loan loan, LibrarySettings settings, DateTime asOfUtc)
    {
        var endDate = (loan.ReturnedAt ?? asOfUtc).Date;
        var overdueDays = (endDate - loan.DueDate.Date).Days;

        if (overdueDays <= 0)
        {
            return 0m;
        }

        var firstTierDays = Math.Min(overdueDays, settings.FineEscalationAfterDays);
        var escalatedDays = Math.Max(0, overdueDays - settings.FineEscalationAfterDays);
        var accrued = (firstTierDays * settings.BaseFinePerDay) + (escalatedDays * settings.EscalatedFinePerDay);

        return Math.Round(accrued, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateOutstanding(
        Loan loan,
        decimal appliedPayments,
        LibrarySettings settings,
        DateTime asOfUtc)
    {
        var accruedFine = CalculateAccruedFine(loan, settings, asOfUtc);
        return Math.Max(0m, accruedFine - appliedPayments);
    }
}
