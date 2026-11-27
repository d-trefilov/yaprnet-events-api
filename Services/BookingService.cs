using EventsApi.DataAccess;
using EventsApi.Dto;
using EventsApi.Exceptions;
using EventsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsApi.Services
{
    public class BookingService(AppDbContext context) : IBookingService
    {
        internal static readonly SemaphoreSlim BookingSemaphore = new(1, 1);

        public async Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            await BookingSemaphore.WaitAsync(cancellationToken);
            try
            {
                var evt = await EventService.FindEventAsync(context.Events, eventId, cancellationToken);
                if (!evt.TryReserveSeats())
                    throw new NoAvailableSeatsException("No available seats for this event");

                var booking = Booking.CreatePending(eventId);
                context.Bookings.Add(booking);
                await context.SaveChangesAsync(cancellationToken);
                return BookingInfo.FromBooking(booking);
            }
            finally
            {
                BookingSemaphore.Release();
            }
        }

        public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
        {
            var booking = await context.Bookings.AsNoTracking()
                .SingleOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);
            if (booking is null) throw new NotFoundException($"Бронь {bookingId} не найдена");

            return BookingInfo.FromBooking(booking);
        }
    }
}
