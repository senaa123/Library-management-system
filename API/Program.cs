using System.Text;
using LibraryM.Application.Abstractions.Authentication;
using LibraryM.Application.Abstractions.Payments;
using LibraryM.Application.Abstractions.Persistence;
using LibraryM.Application.Auth;
using LibraryM.Application.Books;
using LibraryM.Application.Configuration;
using LibraryM.Application.Fines;
using LibraryM.Application.Loans;
using LibraryM.Application.Reservations;
using LibraryM.Application.Transactions;
using LibraryM.Application.Users;
using LibraryM.Infrastructure.Authentication;
using LibraryM.Infrastructure.Payments;
using LibraryM.Infrastructure.Persistence;
using LibraryM.Infrastructure.Persistence.Repositories;
using LibraryM.Infrastructure.Security;
using LibraryM.WebApi.Configuration;
using LibraryM.WebApi.HostedServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Configuration.AddOptionalDotEnv(Path.Combine(builder.Environment.ContentRootPath, ".env"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=library.db";
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");
var librarySettings = builder.Configuration.GetSection(LibrarySettings.SectionName).Get<LibrarySettings>() ?? new LibrarySettings();
var defaultAdminOptions = builder.Configuration.GetSection(DefaultAdminOptions.SectionName).Get<DefaultAdminOptions>() ?? new DefaultAdminOptions();
var stripeOptions = builder.Configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>() ?? new StripeOptions();
var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, ".keys");

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(librarySettings);
builder.Services.AddSingleton(defaultAdminOptions);
builder.Services.AddSingleton(stripeOptions);
builder.Services.AddDbContext<LibraryContext>(options => options.UseSqlite(connectionString));
// Keep app keys inside the project so local runs do not depend on the machine-level profile folder.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

// These registrations keep the Clean Architecture layers wired without pushing data logic into controllers.
builder.Services.AddScoped<ILibraryUnitOfWork, LibraryUnitOfWork>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IFineChargeRepository, FineChargeRepository>();
builder.Services.AddScoped<IFinePaymentRepository, FinePaymentRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IFineCheckoutGateway, StripeFineCheckoutGateway>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IFineService, FineService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<LibraryDatabaseInitializer>();
builder.Services.AddHostedService<ReservationExpiryBackgroundService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Admin", "Librarian"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // The initializer upgrades older SQLite databases and seeds the first admin account when needed.
    var initializer = scope.ServiceProvider.GetRequiredService<LibraryDatabaseInitializer>();
    await initializer.InitializeAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
