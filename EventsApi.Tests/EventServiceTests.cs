using EventsApi.Models;
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

    [Fact]
    public void GetEvents_NoFilters_ReturnsAllEvents()
    {
        // Arrange
        CreateEvent("Event 1");
        CreateEvent("Event 2");
        CreateEvent("Event 3");

        // Act
        var result = _service.GetEvents(null, null, null, 1, 10);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Events.Count());
    }

    [Fact]
    public void GetEvent_ExistingId_ReturnsEvent()
    {
        // Arrange
        var created = CreateEvent("C# meetup");

        // Act
        var found = _service.GetEvent(created.Id);

        // Assert
        Assert.Same(created, found);
    }

    [Fact]
    public void UpdateEvent_ExistingId_UpdatesFields()
    {
        // Arrange
        var created = CreateEvent("Old title");
        var newStart = new DateTime(2026, 12, 1, 9, 0, 0);
        var newEnd = new DateTime(2026, 12, 1, 18, 0, 0);

        // Act
        _service.UpdateEvent(created.Id, "New title", "Description", newStart, newEnd);

        // Assert
        var updated = _service.GetEvent(created.Id);
        Assert.Equal("New title", updated.Title);
        Assert.Equal("Description", updated.Description);
        Assert.Equal(newStart, updated.StartAt);
        Assert.Equal(newEnd, updated.EndAt);
    }

    [Fact]
    public void DeleteEvent_ExistingId_RemovesEvent()
    {
        // Arrange
        var created = CreateEvent("To delete");

        // Act
        _service.DeleteEvent(created.Id);

        // Assert
        var result = _service.GetEvents(null, null, null, 1, 10);
        Assert.Equal(0, result.TotalCount);
    }

    private Event CreateEvent(string title)
    {
        return _service.CreateEvent(
            title,
            null,
            new DateTime(2026, 11, 1, 10, 0, 0),
            new DateTime(2026, 11, 1, 12, 0, 0));
    }
}