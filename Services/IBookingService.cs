using EventsApi.Dto;

namespace EventsApi.Services
{
    public interface IBookingService
    {
        Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    }
}