using EventsApi.Models;

namespace EventsApi.Services
{
    public interface IEventService
    {
        List<Event> GetEvents();
        Event? GetEvent(Guid id);
        Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt);
        Event? UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt);
        bool DeleteEvent(Guid id);


    }
}
