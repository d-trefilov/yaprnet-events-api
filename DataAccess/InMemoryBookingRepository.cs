using System.Collections.Concurrent;
using EventsApi.Models;

namespace EventsApi.DataAccess
{
    public class InMemoryBookingRepository : IBookingRepository
    {
        private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();

        public void Add(Booking booking)
        {
            _bookings[booking.Id] = booking;
        }

        public Booking? GetById(Guid id)
        {
            return _bookings.TryGetValue(id, out var booking) ? booking : null;
        }

        public IReadOnlyList<Booking> GetPending()
        {
            return _bookings.Values
                .Where(b => b.Status == BookingStatus.Pending)
                .ToList();
        }
    }
}