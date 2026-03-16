using LibraryM.Application.Reservations;

namespace LibraryM.WebApi.HostedServices;

public sealed class ReservationExpiryBackgroundService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpiryBackgroundService> _logger;

    public ReservationExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                var expiredCount = await reservationService.ExpireExpiredAsync(stoppingToken);

                if (expiredCount > 0)
                {
                    _logger.LogInformation("Expired {ExpiredCount} stale reservations during the background sweep.", expiredCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Reservation expiry sweep failed.");
            }

            await Task.Delay(SweepInterval, stoppingToken);
        }
    }
}
