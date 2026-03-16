using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Reservations.Models;

public sealed class CreateReservationRequest
{
    [Range(1, int.MaxValue)]
    public int BookId { get; set; }

    public int? MemberId { get; set; }
}
