using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryM.Infrastructure.Persistence;

public sealed class LibraryContext : DbContext
{
    public LibraryContext(DbContextOptions<LibraryContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<FinePayment> FinePayments => Set<FinePayment>();

    public DbSet<TransactionRecord> TransactionRecords => Set<TransactionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(builder =>
        {
            builder.ToTable("Books");
            builder.HasKey(book => book.Id);
            builder.Property(book => book.Title).IsRequired();
            builder.Property(book => book.Author).IsRequired();
            builder.Property(book => book.Description).IsRequired();
            builder.Property(book => book.Category).IsRequired();
            builder.Property(book => book.Isbn).HasDefaultValue(string.Empty);
            builder.Property(book => book.BookType).IsRequired();
            builder.Property(book => book.TotalCopies).HasDefaultValue(1);
            builder.Property(book => book.AvailableCopies).HasDefaultValue(1);
            builder.Property(book => book.IsActive).HasDefaultValue(true);
            builder.HasIndex(book => book.Category);
            builder.HasIndex(book => book.Isbn);
            builder.HasIndex(book => book.IsActive);
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(user => user.Id);
            builder.Property(user => user.Username).IsRequired();
            builder.Property(user => user.PasswordHash).IsRequired();
            builder.Property(user => user.FullName).IsRequired();
            builder.Property(user => user.Email).HasDefaultValue(string.Empty);
            builder.Property(user => user.PhoneNumber).HasDefaultValue(string.Empty);
            builder.Property(user => user.QrCodeValue).IsRequired();
            builder.Property(user => user.Role).HasConversion<string>().IsRequired();
            builder.Property(user => user.IsActive).HasDefaultValue(true);
            builder.HasIndex(user => user.Username).IsUnique();
            builder.HasIndex(user => user.QrCodeValue).IsUnique();
            builder.HasIndex(user => user.Role);
        });

        modelBuilder.Entity<Loan>(builder =>
        {
            builder.ToTable("Loans");
            builder.HasKey(loan => loan.Id);
            builder.Property(loan => loan.Status).HasConversion<string>().IsRequired();
            builder.Property(loan => loan.RenewCount).HasDefaultValue(0);
            builder.HasIndex(loan => new { loan.BookId, loan.Status });
            builder.HasIndex(loan => new { loan.BorrowerId, loan.Status });

            builder.HasOne(loan => loan.Book)
                .WithMany(book => book.Loans)
                .HasForeignKey(loan => loan.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(loan => loan.Borrower)
                .WithMany(user => user.BorrowedLoans)
                .HasForeignKey(loan => loan.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(loan => loan.IssuedBy)
                .WithMany(user => user.IssuedLoans)
                .HasForeignKey(loan => loan.IssuedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reservation>(builder =>
        {
            builder.ToTable("Reservations");
            builder.HasKey(reservation => reservation.Id);
            builder.Property(reservation => reservation.Status).HasConversion<string>().IsRequired();
            builder.HasIndex(reservation => new { reservation.BookId, reservation.Status });
            builder.HasIndex(reservation => new { reservation.MemberId, reservation.Status });

            builder.HasOne(reservation => reservation.Book)
                .WithMany(book => book.Reservations)
                .HasForeignKey(reservation => reservation.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(reservation => reservation.Member)
                .WithMany(user => user.Reservations)
                .HasForeignKey(reservation => reservation.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinePayment>(builder =>
        {
            builder.ToTable("FinePayments");
            builder.HasKey(payment => payment.Id);
            builder.Property(payment => payment.Amount).HasPrecision(18, 2);
            builder.Property(payment => payment.Notes).HasDefaultValue(string.Empty);
            builder.HasIndex(payment => payment.LoanId);
            builder.HasIndex(payment => payment.MemberId);

            builder.HasOne(payment => payment.Loan)
                .WithMany(loan => loan.FinePayments)
                .HasForeignKey(payment => payment.LoanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(payment => payment.Member)
                .WithMany(user => user.FinePayments)
                .HasForeignKey(payment => payment.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(payment => payment.ReceivedBy)
                .WithMany(user => user.CollectedFinePayments)
                .HasForeignKey(payment => payment.ReceivedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TransactionRecord>(builder =>
        {
            builder.ToTable("TransactionRecords");
            builder.HasKey(transaction => transaction.Id);
            builder.Property(transaction => transaction.Type).HasConversion<string>().IsRequired();
            builder.Property(transaction => transaction.Details).HasDefaultValue(string.Empty);
            builder.HasIndex(transaction => transaction.Type);
            builder.HasIndex(transaction => transaction.UserId);
            builder.HasIndex(transaction => transaction.BookId);

            builder.HasOne(transaction => transaction.Book)
                .WithMany()
                .HasForeignKey(transaction => transaction.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(transaction => transaction.User)
                .WithMany()
                .HasForeignKey(transaction => transaction.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(transaction => transaction.PerformedBy)
                .WithMany()
                .HasForeignKey(transaction => transaction.PerformedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(transaction => transaction.Loan)
                .WithMany()
                .HasForeignKey(transaction => transaction.LoanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(transaction => transaction.Reservation)
                .WithMany()
                .HasForeignKey(transaction => transaction.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(transaction => transaction.FinePayment)
                .WithMany()
                .HasForeignKey(transaction => transaction.FinePaymentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
