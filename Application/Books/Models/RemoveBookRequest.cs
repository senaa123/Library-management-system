using System.ComponentModel.DataAnnotations;

namespace LibraryM.Application.Books.Models;

public sealed class RemoveBookRequest
{
    public bool RemoveAllCopies { get; set; }

    [Range(1, int.MaxValue)]
    public int? QuantityToRemove { get; set; }
}
