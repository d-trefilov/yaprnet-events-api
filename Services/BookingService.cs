using EventsApi.DataAccess;
using EventsApi.Dto;
using EventsApi.Exceptions;
using EventsApi.Models;

namespace EventsApi.Services
{
    public class BookingService(IBookingRepository bookingRepository, IEventService eventService) : IBookingService
    {
        public Task<BookingInfo> CreateBookingAsync(Guid eventId)
        {
            // Результат не нужен: GetEvent бросит NotFoundException, если события нет
            eventService.GetEvent(eventId);

            var booking = Booking.CreatePending(eventId);
            bookingRepository.Add(booking);

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