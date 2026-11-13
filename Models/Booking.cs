namespace EventsApi.Models
{
    public class Booking
    {
        public Guid Id { get; private set; }
        public Guid EventId { get; private set; }
        public BookingStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ProcessedAt { get; private set; }

        private Booking() { }

        public static Booking CreatePending(Guid eventId)
        {
            return new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Confirm()
        {
            EnsurePending();
            ProcessedAt = DateTime.UtcNow;
            Status = BookingStatus.Confirmed;
        }

        public void Reject()
        {
            EnsurePending();
            ProcessedAt = DateTime.UtcNow;
            Status = BookingStatus.Rejected;
        }

        private void EnsurePending()
        {
            if (Status != BookingStatus.Pending)
                throw new InvalidOperationException($"Бронь {Id} уже обработана, статус {Status}.");
        }
    }
}