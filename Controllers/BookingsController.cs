using EventsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventsApi.Controllers
{
    [ApiController]
    [Route("bookings")]
    public class BookingsController(IBookingService bookingService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(Guid id)
        {
            var booking = await bookingService.GetBookingByIdAsync(id);
            return Ok(booking);
        }
    }
}