using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Reservations.Models;

public sealed class IssueReservationRequest
{
    [Range(1, 14)]
    public int BorrowDays { get; set; } = 14;
}
