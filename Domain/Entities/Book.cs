namespace LibraryM.Domain.Entities;

public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Isbn { get; set; } = string.Empty;

    public string BookType { get; set; } = "General";

    public int TotalCopies { get; set; } = 1;

    public int AvailableCopies { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
