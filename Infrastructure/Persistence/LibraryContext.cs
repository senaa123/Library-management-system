using LibraryM.Domain.Entities;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(builder =>
        {
            builder.ToTable("Books");
            builder.Property(book => book.Title).IsRequired();
            builder.Property(book => book.Author).IsRequired();
            builder.Property(book => book.Description).IsRequired();
            builder.Property(book => book.Category).IsRequired();
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("Users");
            builder.Property(user => user.Username).IsRequired();
            builder.Property(user => user.PasswordHash).IsRequired();
            builder.Property(user => user.Role).IsRequired();
            builder.HasIndex(user => user.Username).IsUnique();
        });
    }
}
