using EventsApi.Services;

namespace EventsApi.Tests;

public class EventServiceTests
{
    private readonly EventService _service;

    public EventServiceTests()
    {
        _service = new EventService();
    }

    [Fact]
    public void CreateEvent_ValidData_ReturnsCreatedEvent()
    {
        // Arrange
        var title = "C# meetup";
        var startAt = new DateTime(2026, 11, 1, 10, 0, 0);
        var endAt = new DateTime(2026, 11, 1, 12, 0, 0);

        // Act
        var created = _service.CreateEvent(title, null, startAt, endAt);

        // Assert
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(title, created.Title);
        Assert.Equal(startAt, created.StartAt);
        Assert.Equal(endAt, created.EndAt);
        Assert.Same(created, _service.GetEvent(created.Id));
    }
}