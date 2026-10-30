using EventsApi.DataAccess;
using EventsApi.Models;

namespace EventsApi.Services
{
    public class BookingProcessingService(
        IBookingRepository bookingRepository,
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
                    foreach (var booking in bookingRepository.GetPending())
                    {
                        await Task.Delay(ProcessingDelay, stoppingToken);
                        ProcessBooking(booking);
                    }

                    await Task.Delay(PollingInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Штатная остановка приложения — не ошибка
            }

            logger.LogInformation("Обработка броней остановлена");
        }

        private void ProcessBooking(Booking booking)
        {
            try
            {
                booking.Confirm();
                logger.LogInformation("Бронь {BookingId} подтверждена", booking.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Не удалось обработать бронь {BookingId}", booking.Id);
            }
        }
    }
}