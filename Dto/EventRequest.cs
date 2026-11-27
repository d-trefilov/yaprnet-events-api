using System.ComponentModel.DataAnnotations;

namespace EventsApi.Dto
{
    public class EventRequest
    {
        [Required]
        public required string Title { get; init; }
        public string? Description { get; init; }
        public DateTime StartAt { get; init; }
        public DateTime EndAt { get; init; }
        public int TotalSeats { get; init; }
    }
}
