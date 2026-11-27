using EventsApi.Dto;
using EventsApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EventsApi.Controllers
{
    [ApiController]
    [Route("events")]
    public class EventsController(IEventService eventService, IBookingService bookingService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateEvent(EventRequest evt, CancellationToken cancellationToken)
        {
            var evtCreated = await eventService.CreateEventAsync(evt.Title, evt.Description, evt.StartAt, evt.EndAt, evt.TotalSeats, cancellationToken);
            return new CreatedResult($"/events/{evtCreated.Id}", evtCreated);
        }

        [HttpPost("{id}/book")]
        [ProducesResponseType(typeof(BookingInfo), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> BookEvent(Guid id, CancellationToken cancellationToken)
        {
            var booking = await bookingService.CreateBookingAsync(id, cancellationToken);
            return Accepted($"/bookings/{booking.Id}", booking);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken cancellationToken)
        {
            await eventService.DeleteEventAsync(id, cancellationToken);

            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(Guid id, CancellationToken cancellationToken)
        {
            var evt = await eventService.GetEventAsync(id, cancellationToken);

            return Ok(evt);
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents(
            [FromQuery] string? title, 
            [FromQuery] DateTime? from, 
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken, [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            return Ok(await eventService.GetEventsAsync(title, from, to, page, pageSize, cancellationToken));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, EventRequest evt, CancellationToken cancellationToken)
        {
            var evtUpdated = await eventService.UpdateEventAsync(id, evt.Title, evt.Description, evt.StartAt, evt.EndAt, cancellationToken);

            return Ok(evtUpdated);
        }
    }
}
