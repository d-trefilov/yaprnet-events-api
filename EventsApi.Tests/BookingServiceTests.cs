using EventsApi.DataAccess;
using EventsApi.Exceptions;
using EventsApi.Models;
using EventsApi.Services;

namespace EventsApi.Tests;

public class BookingServiceTests
{
    private readonly EventService _eventService;
    private readonly InMemoryBookingRepository _repository;
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _eventService = new EventService();
        _repository = new InMemoryBookingRepository();
        _service = new BookingService(_repository, _eventService);
    }

    private Guid CreateEvent()
    {
        var evt = _eventService.CreateEvent(
            "Концерт", null, new DateTime(2026, 12, 1, 10, 0, 0), new DateTime(2026, 12, 1, 12, 0, 0));
        return evt.Id;
    }

    // ---------- Успешные сценарии ----------

    [Fact]
    public async Task CreateBookingAsync_ExistingEvent_ReturnsPendingBooking()
    {
        // Arrange
        var eventId = CreateEvent();

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
        var eventId = CreateEvent();

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
        var eventId = CreateEvent();
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
        var eventId = CreateEvent();
        var created = await _service.CreateBookingAsync(eventId);
        _repository.GetById(created.Id)!.Confirm();

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
        var eventId = CreateEvent();
        var created = await _service.CreateBookingAsync(eventId);
        _repository.GetById(created.Id)!.Reject();

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
        var eventId = CreateEvent();
        _eventService.DeleteEvent(eventId);

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
}