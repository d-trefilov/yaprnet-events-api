using EventsApi.Dto;

namespace EventsApi.Services
{
    public interface IBookingService
    {
        Task<BookingInfo> CreateBookingAsync(Guid eventId);
        Task<BookingInfo> GetBookingByIdAsync(Guid bookingId);
    }
}