using EventsApi.DataAccess;
using EventsApi.Exceptions;
using EventsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsApi.Services
{
    public class EventService(AppDbContext context) : IEventService
    {
        public async Task<Event> CreateEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats, CancellationToken cancellationToken = default)
        {
            ValidateDates(startAt, endAt);
            ValidateTitle(title);
            if (totalSeats <= 0) throw new ValidationException("Поле TotalSeats должно быть больше нуля.");

            var evt = new Event(title, description, EventTime(startAt), EventTime(endAt), totalSeats);
            context.Events.Add(evt);
            await context.SaveChangesAsync(cancellationToken);
            return evt;
        }

        public async Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await BookingService.BookingSemaphore.WaitAsync(cancellationToken);
            try
            {
                var evt = await FindEventAsync(context.Events, id, cancellationToken);
                evt.IsDeleted = true;
                await context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                BookingService.BookingSemaphore.Release();
            }
        }

        public Task<Event> GetEventAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return FindEventAsync(context.Events.AsNoTracking(), id, cancellationToken);
        }

        public async Task<PaginatedResult<Event>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1 || pageSize < 1)
                throw new ValidationException("Поля page и pageSize не могут быть меньше 1");

            var filtered = context.Events.AsNoTracking().Where(evt => !evt.IsDeleted);

            if (title != null)
            {
                var search = title.ToUpperInvariant();
                filtered = filtered.Where(evt => evt.Title.ToUpper().Contains(search));
            }

            if (from is DateTime fromDate)
                filtered = filtered.Where(evt => evt.StartAt >= EventTime(fromDate));

            if (to is DateTime toDate)
                filtered = filtered.Where(evt => evt.EndAt <= EventTime(toDate));

            var totalCount = await filtered.CountAsync(cancellationToken);
            var offset = (long)(page - 1) * pageSize;
            List<Event> items = [];
            if (offset < totalCount)
            {
                items = await filtered.OrderBy(evt => evt.StartAt)
                    .ThenBy(evt => evt.Title)
                    .ThenBy(evt => evt.Id)
                    .Skip((int)offset)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }

            return new PaginatedResult<Event>
            {
                TotalCount = totalCount,
                Events = items,
                PageNumber = page,
                ItemsOnPage = items.Count
            };
        }

        public async Task<Event> UpdateEventAsync(Guid id, string title, string? description, DateTime startAt, DateTime endAt, CancellationToken cancellationToken = default)
        {
            ValidateDates(startAt, endAt);
            ValidateTitle(title);

            var evt = await FindEventAsync(context.Events, id, cancellationToken);
            evt.Title = title;
            evt.Description = description;
            evt.StartAt = EventTime(startAt);
            evt.EndAt = EventTime(endAt);

            await context.SaveChangesAsync(cancellationToken);
            return evt;
        }

        internal static async Task<Event> FindEventAsync(IQueryable<Event> events, Guid id, CancellationToken cancellationToken)
        {
            return await events.SingleOrDefaultAsync(evt => evt.Id == id && !evt.IsDeleted, cancellationToken)
                ?? throw new NotFoundException($"Событие {id} не найдено");
        }

        private static DateTime EventTime(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

        private static void ValidateDates(DateTime startAt, DateTime endAt)
        {
            if (startAt == default) throw new ValidationException("Поле StartAt не заполнено.");
            if (endAt == default) throw new ValidationException("Поле EndAt не заполнено.");
            if (endAt <= startAt) throw new ValidationException("EndAt должен быть позже StartAt.");
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ValidationException("Поле Title не заполнено.");
            if (title.Length > Event.MaxTitleLength)
                throw new ValidationException($"Поле Title не может быть длиннее {Event.MaxTitleLength} символов.");
        }
    }
}
