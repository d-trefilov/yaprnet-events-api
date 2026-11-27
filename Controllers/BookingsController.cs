using EventsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventsApi.Controllers
{
    [ApiController]
    [Route("bookings")]
    public class BookingsController(IBookingService bookingService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(Guid id, CancellationToken cancellationToken)
        {
            var booking = await bookingService.GetBookingByIdAsync(id, cancellationToken);
            return Ok(booking);
        }
    }
}