using EventsApi.Models;

namespace EventsApi.Services
{
    public class EventService : IEventService
    {
        private List<Event> _events = new List<Event>();

        public Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt)
        {
            Event evt = new Event();

            evt.Id = Guid.NewGuid();
            evt.Title = title;
            evt.Description = description;
            evt.StartAt = startAt;
            evt.EndAt = endAt;

            _events.Add(evt);

            return evt;
        }

        public bool DeleteEvent(Guid id)
        {
            var evt = _events.FirstOrDefault(e => e.Id == id);
            if (evt is null) return false;

            return _events.Remove(evt);
        }

        public Event? GetEvent(Guid id)
        {
            var evt = _events.FirstOrDefault(e => e.Id == id);

            return evt;
        }

        public List<Event> GetEvents()
        {
            return _events;
        }

        public Event? UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
        {
            var evt = _events.FirstOrDefault(e => e.Id == id);
            if (evt is null) return null;

            evt.Title = title;
            evt.Description = description;
            evt.StartAt = startAt;
            evt.EndAt = endAt;

            return evt;
        }
    }
}
