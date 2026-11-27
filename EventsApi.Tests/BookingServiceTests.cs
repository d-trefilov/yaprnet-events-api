using EventsApi.DataAccess;
using EventsApi.Exceptions;
using EventsApi.Models;
using EventsApi.Services;
using Microsoft.EntityFrameworkCore;

namespace EventsApi.Tests;

public class BookingServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly IEventService _eventService;
    private readonly IBookingService _service;
    private readonly AppDbContext _context;

    public BookingServiceTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(_options);
        _eventService = new EventService(_context);
        _service = new BookingService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private async Task<Guid> CreateEvent(int totalSeats = 10)
    {
        var evt = await _eventService.CreateEventAsync(
            "Концерт", null, new DateTime(2026, 12, 1, 10, 0, 0), new DateTime(2026, 12, 1, 12, 0, 0), totalSeats);
        return evt.Id;
    }

    private async Task UpdateBookingAsync(Guid bookingId, Action<Booking> update)
    {
        var booking = await _context.Bookings.Include(item => item.Event).SingleAsync(item => item.Id == bookingId);
        update(booking);
        await _context.SaveChangesAsync();
    }

    // ---------- Успешные сценарии ----------

    [Fact]
    public async Task CreateBookingAsync_ExistingEvent_ReturnsPendingBooking()
    {
        // Arrange
        var eventId = await CreateEvent();

        // Act
        var booking = await _service.CreateBookingAsync(eventId);

        // Assert
        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventId, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_SameEventTwice_ReturnsDifferentIds()
    {
        // Arrange
        var eventId = await CreateEvent();

        // Act
        var first = await _service.CreateBookingAsync(eventId);
        var second = await _service.CreateBookingAsync(eventId);

        // Assert
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingBooking_ReturnsBooking()
    {
        // Arrange
        var eventId = await CreateEvent();
        var created = await _service.CreateBookingAsync(eventId);

        // Act
        var found = await _service.GetBookingByIdAsync(created.Id);

        // Assert
        Assert.Equal(created, found);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterConfirm_ReturnsConfirmedStatus()
    {
        // Arrange
        var eventId = await CreateEvent();
        var created = await _service.CreateBookingAsync(eventId);
        await UpdateBookingAsync(created.Id, booking => booking.Confirm());

        // Act
        var found = await _service.GetBookingByIdAsync(created.Id);

        // Assert
        Assert.Equal(BookingStatus.Confirmed, found.Status);
        Assert.NotNull(found.ProcessedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterReject_ReturnsRejectedStatus()
    {
        // Arrange
        var eventId = await CreateEvent();
        var created = await _service.CreateBookingAsync(eventId);
        await UpdateBookingAsync(created.Id, booking => booking.Reject());

        // Act
        var found = await _service.GetBookingByIdAsync(created.Id);

        // Assert
        Assert.Equal(BookingStatus.Rejected, found.Status);
        Assert.NotNull(found.ProcessedAt);
    }

    // ---------- Неуспешные сценарии ----------

    [Fact]
    public async Task CreateBookingAsync_NonExistingEvent_ThrowsNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateBookingAsync(eventId));
    }

    [Fact]
    public async Task CreateBookingAsync_DeletedEvent_ThrowsNotFound()
    {
        // Arrange
        var eventId = await CreateEvent();
        await _eventService.DeleteEventAsync(eventId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateBookingAsync(eventId));
    }

    [Fact]
    public async Task GetBookingByIdAsync_NonExistingBooking_ThrowsNotFound()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetBookingByIdAsync(bookingId));
    }


    // ---------- Места ----------

    [Fact]
    public async Task CreateBookingAsync_ExistingEvent_DecreasesAvailableSeatsByOne()
    {
        // Arrange
        var eventId = await CreateEvent(totalSeats: 5);

        // Act
        await _service.CreateBookingAsync(eventId);

        // Assert
        Assert.Equal(4, (await _eventService.GetEventAsync(eventId)).AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_UpToLimit_AllSucceedWithUniqueIds()
    {
        // Arrange
        var eventId = await CreateEvent(totalSeats: 3);

        // Act
        var first = await _service.CreateBookingAsync(eventId);
        var second = await _service.CreateBookingAsync(eventId);
        var third = await _service.CreateBookingAsync(eventId);

        // Assert
        var ids = new[] { first.Id, second.Id, third.Id };
        Assert.Equal(3, ids.Distinct().Count());
        Assert.Equal(0, (await _eventService.GetEventAsync(eventId)).AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_AfterSeatsExhausted_ThrowsNoAvailableSeats()
    {
        // Arrange
        var eventId = await CreateEvent(totalSeats: 2);
        await _service.CreateBookingAsync(eventId);
        await _service.CreateBookingAsync(eventId);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _service.CreateBookingAsync(eventId));
    }

    [Fact]
    public async Task CreateBookingAsync_NoAvailableSeats_ThrowsAndKeepsSeatsAtZero()
    {
        // Arrange
        var eventId = await CreateEvent(totalSeats: 1);
        await _service.CreateBookingAsync(eventId);

        // Act
        var ex = await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _service.CreateBookingAsync(eventId));

        // Assert
        Assert.Equal("No available seats for this event", ex.Message);
        Assert.Equal(0, (await _eventService.GetEventAsync(eventId)).AvailableSeats);
    }

    // ---------- Отклонение и возврат места ----------

    [Fact]
    public async Task RejectAndReleaseSeats_RestoresAvailableSeats()
    {
        // Arrange
        var eventId = await CreateEvent(totalSeats: 1);
        var created = await _service.CreateBookingAsync(eventId);

        // Act
        await UpdateBookingAsync(created.Id, booking =>
        {
            booking.Reject();
            booking.Event.ReleaseSeats();
        });

        // Assert
        Assert.Equal(1, (await _eventService.GetEventAsync(eventId)).AvailableSeats);
    }

    [Fact]
    public async Task RejectAndReleaseSeats_AllowsNewBookingForSameSeat()
    {
        // Arrange
        var eventId = await CreateEvent(totalSeats: 1);
        var created = await _service.CreateBookingAsync(eventId);
        await UpdateBookingAsync(created.Id, booking =>
        {
            booking.Reject();
            booking.Event.ReleaseSeats();
        });

        // Act
        var second = await _service.CreateBookingAsync(eventId);

        // Assert
        Assert.Equal(BookingStatus.Pending, second.Status);
        Assert.NotEqual(created.Id, second.Id);
    }

    // ---------- Конкурентность ----------

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_NoOverbooking()
    {
        // Arrange
        var eventId = await CreateEvent(totalSeats: 5);

        // Act
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                using var context = new AppDbContext(_options);
                var service = new BookingService(context);
                try
                {
                    await service.CreateBookingAsync(eventId);
                    return true;
                }
                catch (NoAvailableSeatsException)
                {
                    return false;
                }
            }));
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, results.Count(success => success));
        Assert.Equal(15, results.Count(success => !success));
        Assert.Equal(0, (await _eventService.GetEventAsync(eventId)).AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_ReturnUniqueIds()
    {
        // Arrange
        var eventId = await CreateEvent(totalSeats: 10);

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                using var context = new AppDbContext(_options);
                var service = new BookingService(context);
                return await service.CreateBookingAsync(eventId);
            }));
        var bookings = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, bookings.Select(b => b.Id).Distinct().Count());
    }
}