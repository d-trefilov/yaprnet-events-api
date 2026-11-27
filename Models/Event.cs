using System.Text.Json.Serialization;

namespace EventsApi.Models
{
    public class Event
    {
        internal const int MaxTitleLength = 200;

        public Guid Id { get; private set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int TotalSeats { get; private set; }
        public int AvailableSeats { get; private set; }
        internal bool IsDeleted { get; set; }

        [JsonIgnore]
        public ICollection<Booking> Bookings { get; } = new List<Booking>();

        private Event() { }

        public Event(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
        {
            Id = Guid.NewGuid();
            Title = title;
            Description = description;
            StartAt = startAt;
            EndAt = endAt;
            TotalSeats = totalSeats;
            AvailableSeats = totalSeats;
        }

        public bool TryReserveSeats(int count = 1)
        {
            if (AvailableSeats < count) return false;
            AvailableSeats -= count;
            return true;
        }

        public void ReleaseSeats(int count = 1)
        {
            AvailableSeats = Math.Min(AvailableSeats + count, TotalSeats);
        }
    }
}
