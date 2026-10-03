using System.ComponentModel.DataAnnotations;

namespace EventsApi.Models
{
    public class Event
    {
        public Guid Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

    }
}
