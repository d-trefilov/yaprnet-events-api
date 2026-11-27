using EventsApi.DataAccess;
using EventsApi.Dto;
using EventsApi.Exceptions;
using EventsApi.Models;

namespace EventsApi.Services
{
    public class BookingService(IBookingRepository bookingRepository, IEventService eventService) : IBookingService
    {
        private readonly object _bookingLock = new();

        public Task<BookingInfo> CreateBookingAsync(Guid eventId)
        {
            Booking booking;

            lock (_bookingLock)
            {
                var evt = eventService.GetEvent(eventId);

                if (!evt.TryReserveSeats())
                    throw new NoAvailableSeatsException("No available seats for this event");

                booking = Booking.CreatePending(eventId);
                bookingRepository.Add(booking);
            }

            return Task.FromResult(BookingInfo.FromBooking(booking));
        }

        public Task<BookingInfo> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = bookingRepository.GetById(bookingId);
            if (booking is null) throw new NotFoundException($"Бронь {bookingId} не найдена");

            return Task.FromResult(BookingInfo.FromBooking(booking));
        }
    }
}