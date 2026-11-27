using EventsApi.DataAccess;
using EventsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsApi.Services
{
    public class BookingProcessingService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingProcessingService> logger) : BackgroundService
    {
        private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Обработка броней запущена");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var bookingIds = await GetPendingIdsAsync(stoppingToken);
                        await Task.WhenAll(bookingIds.Select(id => ProcessBookingAsync(id, stoppingToken)));
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogError(ex, "Не удалось обработать очередь броней");
                    }

                    await Task.Delay(PollingInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Штатная остановка приложения.
            }

            logger.LogInformation("Обработка броней остановлена");
        }

        private async Task<List<Guid>> GetPendingIdsAsync(CancellationToken stoppingToken)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await context.Bookings.AsNoTracking()
                .Where(booking => booking.Status == BookingStatus.Pending)
                .Select(booking => booking.Id)
                .ToListAsync(stoppingToken);
        }

        private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            logger.LogInformation("Начата обработка брони {BookingId}", bookingId);
            await Task.Delay(ProcessingDelay, stoppingToken);
            await SaveResultAsync(bookingId, stoppingToken);
        }

        private async Task SaveResultAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await BookingService.BookingSemaphore.WaitAsync(stoppingToken);
            try
            {
                var booking = await context.Bookings.Include(booking => booking.Event)
                    .SingleOrDefaultAsync(booking => booking.Id == bookingId, stoppingToken);
                if (booking?.Status != BookingStatus.Pending) return;

                if (booking.Event.IsDeleted)
                {
                    booking.Reject();
                }
                else
                {
                    booking.Confirm();
                }

                await context.SaveChangesAsync(stoppingToken);
                logger.LogInformation("Бронь {BookingId}: {Status}", booking.Id, booking.Status);
            }
            finally
            {
                BookingService.BookingSemaphore.Release();
            }
        }
    }
}
