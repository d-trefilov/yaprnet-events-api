using EventsApi.Models;
using EventsApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EventsApi.Controllers
{
    [ApiController]
    [Route("events")]
    public class EventsController(IEventService eventService) : ControllerBase
    {
        [HttpPost]
        public IActionResult CreateEvent(Event evt)
        {
            var evtCreated = eventService.CreateEvent(evt.Title, evt.Description, evt.StartAt, evt.EndAt);
            return new CreatedResult($"/events/{evtCreated.Id}", evtCreated);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteEvent(Guid id)
        {
            eventService.DeleteEvent(id);

            return NoContent();
        }

        [HttpGet("{id}")]
        public IActionResult GetEvent(Guid id)
        {
            var evt = eventService.GetEvent(id);

            return Ok(evt);
        }

        [HttpGet]
        public IActionResult GetEvents(
            [FromQuery] string? title, 
            [FromQuery] DateTime? from, 
            [FromQuery] DateTime? to)
        {
            return Ok(eventService.GetEvents(title, from, to));
        }

        [HttpPut("{id}")]
        public IActionResult UpdateEvent(Guid id, Event evt)
        {
            var evtUpdated = eventService.UpdateEvent(id, evt.Title, evt.Description, evt.StartAt, evt.EndAt);

            return Ok(evtUpdated);
        }
    }
}
