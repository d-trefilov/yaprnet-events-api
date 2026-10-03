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
            ValidateEvent(evt);
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var evtCreated = eventService.CreateEvent(evt.Title, evt.Description, evt.StartAt, evt.EndAt);
            return new CreatedResult($"/events/{evtCreated.Id}", evtCreated);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteEvent(Guid id)
        {
            var evtDeleted = eventService.DeleteEvent(id);
            if (!evtDeleted) return NotFound();

            return NoContent();
        }

        [HttpGet("{id}")]
        public IActionResult GetEvent(Guid id)
        {
            var evt = eventService.GetEvent(id);
            if (evt is null) return NotFound();

            return Ok(evt);
        }

        [HttpGet]
        public IActionResult GetEvents()
        {
            return Ok(eventService.GetEvents());
        }

        [HttpPut("{id}")]
        public IActionResult UpdateEvent(Guid id, Event evt)
        {
            ValidateEvent(evt);
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var evtUpdated = eventService.UpdateEvent(id, evt.Title, evt.Description, evt.StartAt, evt.EndAt);
            if (evtUpdated is null) return NotFound();

            return Ok(evtUpdated);
        }

        private void ValidateEvent(Event evt)
        {
            if (evt.StartAt == default)
            {
                ModelState.AddModelError(nameof(evt.StartAt), "Поле StartAt обязательно.");
            }

            if (evt.EndAt == default)
            {
                ModelState.AddModelError(nameof(evt.EndAt), "Поле EndAt обязательно.");
            }

            if (evt.EndAt <= evt.StartAt)
            {
                ModelState.AddModelError(nameof(evt.EndAt), "EndAt должен быть позже StartAt.");
            }
        }
    }
}
