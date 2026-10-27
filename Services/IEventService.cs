using EventsApi.Models;

namespace EventsApi.Services
{
    public interface IEventService
    {
        PaginatedResult<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize);
        Event GetEvent(Guid id);
        Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt);
        Event UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt);
        void DeleteEvent(Guid id);

    }
}
