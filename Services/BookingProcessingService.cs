using EventsApi.DataAccess;
using EventsApi.Exceptions;
using EventsApi.Models;

namespace EventsApi.Services
{
    public class BookingProcessingService(
        IBookingRepository bookingRepository,
        IEventService eventService,
        ILogger<BookingProcessingService> logger) : BackgroundService
    {
        private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Обработка броней запущена");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var pendingBookings = bookingRepository.GetPending().ToList();
                    var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                    await Task.WhenAll(tasks);

                    await Task.Delay(PollingInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Штатная остановка приложения — не ошибка
            }

            logger.LogInformation("Обработка броней остановлена");
        }

        private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
        {
            logger.LogInformation(
                "Начата обработка брони {BookingId}, поток {ThreadId}",
                booking.Id, Environment.CurrentManagedThreadId);

            try
            {
                // Имитация внешнего вызова — до семафора, чтобы задержки шли параллельно
                await Task.Delay(ProcessingDelay, stoppingToken);

                await _processingSemaphore.WaitAsync(stoppingToken);
                try
                {
                    var evt = FindEvent(booking.EventId);
                    if (evt is null)
                    {
                        booking.Reject();
                        bookingRepository.Update(booking);
                        logger.LogWarning(
                            "Бронь {BookingId} отклонена: событие {EventId} удалено",
                            booking.Id, booking.EventId);
                        return;
                    }

                    booking.Confirm();
                    bookingRepository.Update(booking);
                    logger.LogInformation("Бронь {BookingId} подтверждена", booking.Id);
                }
                finally
                {
                    _processingSemaphore.Release();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Остановка приложения: бронь остаётся Pending
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Не удалось обработать бронь {BookingId}", booking.Id);
                RejectAndReleaseSeat(booking);
            }
        }

        private void RejectAndReleaseSeat(Booking booking)
        {
            if (booking.Status != BookingStatus.Pending) return;

            booking.Reject();
            bookingRepository.Update(booking);
            FindEvent(booking.EventId)?.ReleaseSeats();
        }

        private Event? FindEvent(Guid eventId)
        {
            try
            {
                return eventService.GetEvent(eventId);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }
    }
}