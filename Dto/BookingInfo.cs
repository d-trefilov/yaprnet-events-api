using EventsApi.Models;

namespace EventsApi.Dto
{
    public record BookingInfo(
        Guid Id,
        Guid EventId,
        BookingStatus Status,
        DateTime CreatedAt,
        DateTime? ProcessedAt)
    {
        public static BookingInfo FromBooking(Booking booking) =>
            new(booking.Id, booking.EventId, booking.Status, booking.CreatedAt, booking.ProcessedAt);
    }
}