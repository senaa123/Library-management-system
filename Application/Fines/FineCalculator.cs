using LibraryM.Domain.Entities;

namespace LibraryM.Application.Fines;

public static class FineCalculator
{
    public static decimal CalculateAccruedFine(Loan loan, decimal finePerDay, DateTime asOfUtc)
    {
        if (finePerDay <= 0m)
        {
            return 0m;
        }

        var endDate = (loan.ReturnedAt ?? asOfUtc).Date;
        var overdueDays = (endDate - loan.DueDate.Date).Days;

        return overdueDays > 0 ? overdueDays * finePerDay : 0m;
    }

    public static decimal CalculateOutstanding(
        Loan loan,
        IEnumerable<FinePayment>? payments,
        decimal finePerDay,
        DateTime asOfUtc)
    {
        var accruedFine = CalculateAccruedFine(loan, finePerDay, asOfUtc);
        var paidAmount = payments?.Sum(payment => payment.Amount) ?? 0m;
        return Math.Max(0m, accruedFine - paidAmount);
    }
}
