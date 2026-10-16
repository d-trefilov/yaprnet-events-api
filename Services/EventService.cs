using EventsApi.Models;
using EventsApi.Exceptions;

namespace EventsApi.Services
{
    public class EventService : IEventService
    {
        private List<Event> _events = new List<Event>();

        public Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt)
        {
            ValidateDates(startAt, endAt);


            var evt = new Event
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                StartAt = startAt,
                EndAt = endAt
            };

            _events.Add(evt);

            return evt;
        }

        public void DeleteEvent(Guid id)
        {
            var evt = _events.FirstOrDefault(e => e.Id == id);
            if (evt is null) throw new NotFoundException($"Событие {id} не найдено");

            _events.Remove(evt);
        }

        public Event GetEvent(Guid id)
        {
            var evt = _events.FirstOrDefault(e => e.Id == id);
            if (evt is null) throw new NotFoundException($"Событие {id} не найдено");

            return evt;
        }

        public List<Event> GetEvents()
        {
            return _events;
        }

        public Event UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
        {
            ValidateDates(startAt, endAt);

            var evt = _events.FirstOrDefault(e => e.Id == id);
            if (evt is null) throw new NotFoundException($"Событие {id} не найдено");

            evt.Title = title;
            evt.Description = description;
            evt.StartAt = startAt;
            evt.EndAt = endAt;

            return evt;
        }

        private void ValidateDates(DateTime startAt, DateTime endAt)
        {
            if (startAt == default) throw new ValidationException("Поле StartAt не заполнено.");
            if (endAt == default) throw new ValidationException("Поле EndAt не заполнено.");
            if (endAt <= startAt) throw new ValidationException("EndAt должен быть позже StartAt.");
        }
    }
}
