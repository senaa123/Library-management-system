using LibraryM.Application.Abstractions.Authentication;
using LibraryM.Domain.Entities;
using LibraryM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryM.Infrastructure.Persistence;

public sealed class LibraryDatabaseInitializer
{
    private readonly LibraryContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly DefaultAdminOptions _defaultAdminOptions;

    public LibraryDatabaseInitializer(
        LibraryContext dbContext,
        IPasswordHasher passwordHasher,
        DefaultAdminOptions defaultAdminOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _defaultAdminOptions = defaultAdminOptions;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

        await EnsureBookColumnsAsync(cancellationToken);
        await EnsureUserColumnsAsync(cancellationToken);
        await EnsureLoansTableAsync(cancellationToken);
        await EnsureReservationsTableAsync(cancellationToken);
        await EnsureFinePaymentsTableAsync(cancellationToken);
        await EnsureTransactionRecordsTableAsync(cancellationToken);
        await NormalizeLegacyDataAsync(cancellationToken);
        await SeedDefaultAdminAsync(cancellationToken);
    }

    private async Task EnsureBookColumnsAsync(CancellationToken cancellationToken)
    {
        await EnsureColumnAsync("Books", "Isbn", "TEXT NOT NULL DEFAULT ''", cancellationToken);
        await EnsureColumnAsync("Books", "BookType", "TEXT NOT NULL DEFAULT 'General'", cancellationToken);
        await EnsureColumnAsync("Books", "TotalCopies", "INTEGER NOT NULL DEFAULT 1", cancellationToken);
        await EnsureColumnAsync("Books", "AvailableCopies", "INTEGER NOT NULL DEFAULT 1", cancellationToken);
        await EnsureColumnAsync("Books", "IsActive", "INTEGER NOT NULL DEFAULT 1", cancellationToken);
        await EnsureColumnAsync("Books", "CreatedAt", "TEXT NULL", cancellationToken);

        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_Books_Category ON Books(Category);", cancellationToken);
        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_Books_Isbn ON Books(Isbn);", cancellationToken);
        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_Books_IsActive ON Books(IsActive);", cancellationToken);
    }

    private async Task EnsureUserColumnsAsync(CancellationToken cancellationToken)
    {
        await EnsureColumnAsync("Users", "FullName", "TEXT NOT NULL DEFAULT ''", cancellationToken);
        await EnsureColumnAsync("Users", "Email", "TEXT NOT NULL DEFAULT ''", cancellationToken);
        await EnsureColumnAsync("Users", "PhoneNumber", "TEXT NOT NULL DEFAULT ''", cancellationToken);
        await EnsureColumnAsync("Users", "IsActive", "INTEGER NOT NULL DEFAULT 1", cancellationToken);
        await EnsureColumnAsync("Users", "CreatedAt", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync("Users", "UpdatedAt", "TEXT NULL", cancellationToken);

        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_Users_Role ON Users(Role);", cancellationToken);
    }

    private async Task EnsureLoansTableAsync(CancellationToken cancellationToken)
    {
        await ExecuteSqlAsync(
            """
            CREATE TABLE IF NOT EXISTS Loans (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                BookId INTEGER NOT NULL,
                BorrowerId INTEGER NOT NULL,
                IssuedById INTEGER NOT NULL,
                IssuedAt TEXT NOT NULL,
                DueDate TEXT NOT NULL,
                ReturnedAt TEXT NULL,
                RenewCount INTEGER NOT NULL DEFAULT 0,
                Status TEXT NOT NULL,
                FOREIGN KEY (BookId) REFERENCES Books(Id) ON DELETE RESTRICT,
                FOREIGN KEY (BorrowerId) REFERENCES Users(Id) ON DELETE RESTRICT,
                FOREIGN KEY (IssuedById) REFERENCES Users(Id) ON DELETE RESTRICT
            );
            """,
            cancellationToken);

        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_Loans_BookId_Status ON Loans(BookId, Status);", cancellationToken);
        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_Loans_BorrowerId_Status ON Loans(BorrowerId, Status);", cancellationToken);
    }

    private async Task EnsureReservationsTableAsync(CancellationToken cancellationToken)
    {
        await ExecuteSqlAsync(
            """
            CREATE TABLE IF NOT EXISTS Reservations (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                BookId INTEGER NOT NULL,
                MemberId INTEGER NOT NULL,
                ReservedAt TEXT NOT NULL,
                NotifiedAt TEXT NULL,
                CancelledAt TEXT NULL,
                FulfilledAt TEXT NULL,
                Status TEXT NOT NULL,
                FOREIGN KEY (BookId) REFERENCES Books(Id) ON DELETE RESTRICT,
                FOREIGN KEY (MemberId) REFERENCES Users(Id) ON DELETE RESTRICT
            );
            """,
            cancellationToken);

        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_Reservations_BookId_Status ON Reservations(BookId, Status);", cancellationToken);
        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_Reservations_MemberId_Status ON Reservations(MemberId, Status);", cancellationToken);
    }

    private async Task EnsureFinePaymentsTableAsync(CancellationToken cancellationToken)
    {
        await ExecuteSqlAsync(
            """
            CREATE TABLE IF NOT EXISTS FinePayments (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                LoanId INTEGER NOT NULL,
                MemberId INTEGER NOT NULL,
                ReceivedById INTEGER NOT NULL,
                Amount TEXT NOT NULL,
                PaidAt TEXT NOT NULL,
                Notes TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (LoanId) REFERENCES Loans(Id) ON DELETE RESTRICT,
                FOREIGN KEY (MemberId) REFERENCES Users(Id) ON DELETE RESTRICT,
                FOREIGN KEY (ReceivedById) REFERENCES Users(Id) ON DELETE RESTRICT
            );
            """,
            cancellationToken);

        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_FinePayments_LoanId ON FinePayments(LoanId);", cancellationToken);
        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_FinePayments_MemberId ON FinePayments(MemberId);", cancellationToken);
    }

    private async Task EnsureTransactionRecordsTableAsync(CancellationToken cancellationToken)
    {
        await ExecuteSqlAsync(
            """
            CREATE TABLE IF NOT EXISTS TransactionRecords (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Type TEXT NOT NULL,
                BookId INTEGER NULL,
                UserId INTEGER NULL,
                PerformedById INTEGER NULL,
                LoanId INTEGER NULL,
                ReservationId INTEGER NULL,
                FinePaymentId INTEGER NULL,
                Details TEXT NOT NULL DEFAULT '',
                OccurredAt TEXT NOT NULL,
                FOREIGN KEY (BookId) REFERENCES Books(Id) ON DELETE RESTRICT,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE RESTRICT,
                FOREIGN KEY (PerformedById) REFERENCES Users(Id) ON DELETE RESTRICT,
                FOREIGN KEY (LoanId) REFERENCES Loans(Id) ON DELETE RESTRICT,
                FOREIGN KEY (ReservationId) REFERENCES Reservations(Id) ON DELETE RESTRICT,
                FOREIGN KEY (FinePaymentId) REFERENCES FinePayments(Id) ON DELETE RESTRICT
            );
            """,
            cancellationToken);

        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_TransactionRecords_Type ON TransactionRecords(Type);", cancellationToken);
        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_TransactionRecords_UserId ON TransactionRecords(UserId);", cancellationToken);
        await ExecuteSqlAsync("CREATE INDEX IF NOT EXISTS IX_TransactionRecords_BookId ON TransactionRecords(BookId);", cancellationToken);
    }

    private async Task NormalizeLegacyDataAsync(CancellationToken cancellationToken)
    {
        await ExecuteSqlAsync("UPDATE Users SET Role = 'Member' WHERE Role IS NULL OR TRIM(Role) = '' OR Role = 'User';", cancellationToken);
        await ExecuteSqlAsync("UPDATE Users SET FullName = Username WHERE FullName IS NULL OR TRIM(FullName) = '';", cancellationToken);
        await ExecuteSqlAsync("UPDATE Users SET Email = '' WHERE Email IS NULL;", cancellationToken);
        await ExecuteSqlAsync("UPDATE Users SET PhoneNumber = '' WHERE PhoneNumber IS NULL;", cancellationToken);
        await ExecuteSqlAsync("UPDATE Users SET IsActive = 1 WHERE IsActive IS NULL;", cancellationToken);
        await ExecuteSqlAsync("UPDATE Users SET CreatedAt = CURRENT_TIMESTAMP WHERE CreatedAt IS NULL OR TRIM(CreatedAt) = '';", cancellationToken);

        await ExecuteSqlAsync("UPDATE Books SET Isbn = '' WHERE Isbn IS NULL;", cancellationToken);
        await ExecuteSqlAsync("UPDATE Books SET BookType = 'General' WHERE BookType IS NULL OR TRIM(BookType) = '';", cancellationToken);
        await ExecuteSqlAsync("UPDATE Books SET TotalCopies = 1 WHERE TotalCopies IS NULL OR TotalCopies < 1;", cancellationToken);
        await ExecuteSqlAsync("UPDATE Books SET AvailableCopies = TotalCopies WHERE AvailableCopies IS NULL;", cancellationToken);
        await ExecuteSqlAsync("UPDATE Books SET AvailableCopies = 0 WHERE AvailableCopies < 0;", cancellationToken);
        await ExecuteSqlAsync("UPDATE Books SET AvailableCopies = TotalCopies WHERE AvailableCopies > TotalCopies;", cancellationToken);
        await ExecuteSqlAsync("UPDATE Books SET IsActive = 1 WHERE IsActive IS NULL;", cancellationToken);
        await ExecuteSqlAsync("UPDATE Books SET CreatedAt = CURRENT_TIMESTAMP WHERE CreatedAt IS NULL OR TRIM(CreatedAt) = '';", cancellationToken);
    }

    private async Task SeedDefaultAdminAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_defaultAdminOptions.Username) || string.IsNullOrWhiteSpace(_defaultAdminOptions.Password))
        {
            return;
        }

        var adminExists = await _dbContext.Users.AnyAsync(user => user.Role == UserRole.Admin, cancellationToken);
        if (adminExists)
        {
            return;
        }

        var username = _defaultAdminOptions.Username.Trim();
        var usernameExists = await _dbContext.Users.AnyAsync(user => user.Username.ToLower() == username.ToLower(), cancellationToken);
        if (usernameExists)
        {
            return;
        }

        _dbContext.Users.Add(
            new User
            {
                Username = username,
                PasswordHash = _passwordHasher.Hash(_defaultAdminOptions.Password),
                FullName = string.IsNullOrWhiteSpace(_defaultAdminOptions.FullName) ? username : _defaultAdminOptions.FullName.Trim(),
                Email = _defaultAdminOptions.Email?.Trim() ?? string.Empty,
                PhoneNumber = _defaultAdminOptions.PhoneNumber?.Trim() ?? string.Empty,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureColumnAsync(string tableName, string columnName, string columnDefinition, CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(tableName, columnName, cancellationToken))
        {
            return;
        }

        await ExecuteSqlAsync($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};", cancellationToken);
    }

    private async Task<bool> ColumnExistsAsync(string tableName, string columnName, CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName});";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var existingColumn = reader.GetString(1);
                if (string.Equals(existingColumn, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private Task ExecuteSqlAsync(string sql, CancellationToken cancellationToken) =>
        _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
}
