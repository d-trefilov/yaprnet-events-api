using EventsApi.Exceptions;
using EventsApi.Models;

namespace EventsApi.Services
{
    public class EventService : IEventService
    {
        private List<Event> _events = new List<Event>();

        public Event CreateEvent(string title, string? description, DateTime startAt, DateTime endAt)
        {
            ValidateDates(startAt, endAt);
            ValidateTitle(title);

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

        public PaginatedResult<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)  throw new ValidationException("Поля page и pageSize не могут быть меньше 1"); 

            var filtered = _events.AsEnumerable();

            if (title != null)
            {
                filtered = filtered.Where(ev => ev.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
            }
            if (from != null)
            {
                filtered = filtered.Where(ev => ev.StartAt >= from);
            }
            if (to != null)
            {
                filtered = filtered.Where(ev => ev.EndAt <= to);
            }
            
            var totalCount = filtered.Count();
            var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PaginatedResult<Event>
            {
                TotalCount = totalCount,
                Events = items,
                PageNumber = page,
                ItemsOnPage = items.Count
            };

        }

        public Event UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
        {
            ValidateDates(startAt, endAt);
            ValidateTitle(title);

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

        private void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ValidationException("Поле Title не заполнено.");
        }
    }
}
