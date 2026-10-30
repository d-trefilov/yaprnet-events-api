using EventsApi.Models;

namespace EventsApi.DataAccess
{
    public interface IBookingRepository
    {
        void Add(Booking booking);
        Booking? GetById(Guid id);
        IReadOnlyList<Booking> GetPending();
    }
}