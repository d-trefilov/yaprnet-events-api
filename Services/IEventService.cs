using EventsApi.Models;

namespace EventsApi.Services
{
    public interface IEventService
    {
        Task<PaginatedResult<Event>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<Event> GetEventAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Event> CreateEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats, CancellationToken cancellationToken = default);
        Task<Event> UpdateEventAsync(Guid id, string title, string? description, DateTime startAt, DateTime endAt, CancellationToken cancellationToken = default);
        Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
