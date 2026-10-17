using EventsApi.Exceptions;
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

    [Fact]
    public void GetEvents_TitleFilter_ReturnsMatchingIgnoringCase()
    {
        // Arrange
        SeedThreeEvents();

        // Act
        var result = _service.GetEvents("c#", null, null, 1, 10);

        // Assert
        var titles = result.Events.Select(e => e.Title).ToList();
        Assert.Equal(2, result.TotalCount);
        Assert.All(titles, t => Assert.Contains("c#", t, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("Kotlin day", titles);
    }

    [Fact]
    public void GetEvents_DateFilter_ReturnsEventsInRange()
    {
        // Arrange
        SeedThreeEvents();
        var from = new DateTime(2026, 11, 5);
        var to = new DateTime(2026, 11, 30);

        // Act
        var result = _service.GetEvents(null, from, to, 1, 10);

        // Assert
        var single = Assert.Single(result.Events);
        Assert.Equal("Kotlin day", single.Title);
    }

    [Fact]
    public void GetEvents_Pagination_ReturnsRequestedPage()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
        {
            CreateEvent($"Event {i}");
        }

        // Act
        var result = _service.GetEvents(null, null, null, 2, 2);

        // Assert
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.ItemsOnPage);
        Assert.Equal(["Event 3", "Event 4"], result.Events.Select(e => e.Title));
    }

    [Fact]
    public void GetEvents_CombinedFilters_ReturnsIntersection()
    {
        // Arrange
        SeedThreeEvents();
        var from = new DateTime(2026, 11, 5);

        // Act
        var result = _service.GetEvents("c#", from, null, 1, 10);

        // Assert
        var single = Assert.Single(result.Events);
        Assert.Equal("Advanced C#", single.Title);
    }

    private Event CreateEvent(string title)
    {
        return _service.CreateEvent(
            title,
            null,
            new DateTime(2026, 11, 1, 10, 0, 0),
            new DateTime(2026, 11, 1, 12, 0, 0));
    }

    [Fact]
    public void GetEvent_NonExistingId_ThrowsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<NotFoundException>(() => _service.GetEvent(id));
        Assert.Contains(id.ToString(), ex.Message);
    }

    [Fact]
    public void UpdateEvent_NonExistingId_ThrowsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var startAt = new DateTime(2026, 11, 1, 10, 0, 0);
        var endAt = new DateTime(2026, 11, 1, 12, 0, 0);

        // Act & Assert
        Assert.Throws<NotFoundException>(
            () => _service.UpdateEvent(id, "Title", null, startAt, endAt));
    }

    [Fact]
    public void CreateEvent_EndBeforeStart_ThrowsValidation()
    {
        // Arrange
        var startAt = new DateTime(2026, 11, 1, 12, 0, 0);
        var endAt = new DateTime(2026, 11, 1, 10, 0, 0);

        // Act & Assert
        Assert.Throws<ValidationException>(
            () => _service.CreateEvent("Title", null, startAt, endAt));
    }

    [Fact]
    public void UpdateEvent_EndBeforeStart_ThrowsValidation()
    {
        // Arrange
        var created = CreateEvent("C# meetup");
        var startAt = new DateTime(2026, 11, 1, 12, 0, 0);
        var endAt = new DateTime(2026, 11, 1, 10, 0, 0);

        // Act & Assert
        Assert.Throws<ValidationException>(
            () => _service.UpdateEvent(created.Id, "C# meetup", null, startAt, endAt));
    }

    [Fact]
    public void GetEvents_InvalidPage_ThrowsValidation()
    {
        // Act & Assert
        Assert.Throws<ValidationException>(
            () => _service.GetEvents(null, null, null, 0, 10));
    }

    private Event CreateEvent(string title, DateTime startAt, DateTime endAt)
    {
        return _service.CreateEvent(title, null, startAt, endAt);
    }

    private void SeedThreeEvents()
    {
        CreateEvent("C# meetup", new DateTime(2026, 11, 1, 10, 0, 0), new DateTime(2026, 11, 1, 12, 0, 0));
        CreateEvent("Kotlin day", new DateTime(2026, 11, 10, 10, 0, 0), new DateTime(2026, 11, 10, 18, 0, 0));
        CreateEvent("Advanced C#", new DateTime(2026, 12, 5, 9, 0, 0), new DateTime(2026, 12, 6, 18, 0, 0));
    }
}