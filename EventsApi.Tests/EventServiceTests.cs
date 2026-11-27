using EventsApi.Exceptions;
using EventsApi.Models;
using EventsApi.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventsApi.Tests;

public class EventServiceTests : IDisposable
{
    private readonly ServiceProvider _provider = TestServices.Create();
    private readonly IServiceScope _scope;
    private readonly IEventService _service;

    public EventServiceTests()
    {
        _scope = _provider.CreateScope();
        _service = _scope.ServiceProvider.GetRequiredService<IEventService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public async Task CreateEvent_ValidData_ReturnsCreatedEvent()
    {
        // Arrange
        var title = "C# meetup";
        var startAt = new DateTime(2026, 11, 1, 10, 0, 0);
        var endAt = new DateTime(2026, 11, 1, 12, 0, 0);

        // Act
        var created = await _service.CreateEventAsync(title, null, startAt, endAt, 10);

        // Assert
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(title, created.Title);
        Assert.Equal(startAt, created.StartAt);
        Assert.Equal(endAt, created.EndAt);
        Assert.Equal(10, created.TotalSeats);       // новое
        Assert.Equal(10, created.AvailableSeats);   // новое
        Assert.Equal(created.Id, (await _service.GetEventAsync(created.Id)).Id);
    }

    [Fact]
    public async Task GetEvents_NoFilters_ReturnsAllEvents()
    {
        // Arrange
        await CreateEvent("Event 1");
        await CreateEvent("Event 2");
        await CreateEvent("Event 3");

        // Act
        var result = await _service.GetEventsAsync(null, null, null, 1, 10);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Events.Count());
    }

    [Fact]
    public async Task GetEvent_ExistingId_ReturnsEvent()
    {
        // Arrange
        var created = await CreateEvent("C# meetup");

        // Act
        var found = await _service.GetEventAsync(created.Id);

        // Assert
        Assert.Equal(created.Id, found.Id);
        Assert.Equal(created.Title, found.Title);
    }

    [Fact]
    public async Task UpdateEvent_ExistingId_UpdatesFields()
    {
        // Arrange
        var created = await CreateEvent("Old title");
        var newStart = new DateTime(2026, 12, 1, 9, 0, 0);
        var newEnd = new DateTime(2026, 12, 1, 18, 0, 0);

        // Act
        await _service.UpdateEventAsync(created.Id, "New title", "Description", newStart, newEnd);

        // Assert
        var updated = await _service.GetEventAsync(created.Id);
        Assert.Equal("New title", updated.Title);
        Assert.Equal("Description", updated.Description);
        Assert.Equal(newStart, updated.StartAt);
        Assert.Equal(newEnd, updated.EndAt);
    }

    [Fact]
    public async Task DeleteEvent_ExistingId_RemovesEvent()
    {
        // Arrange
        var created = await CreateEvent("To delete");

        // Act
        await _service.DeleteEventAsync(created.Id);

        // Assert
        var result = await _service.GetEventsAsync(null, null, null, 1, 10);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetEvents_TitleFilter_ReturnsMatchingIgnoringCase()
    {
        // Arrange
        await SeedThreeEvents();

        // Act
        var result = await _service.GetEventsAsync("c#", null, null, 1, 10);

        // Assert
        var titles = result.Events.Select(e => e.Title).ToList();
        Assert.Equal(2, result.TotalCount);
        Assert.All(titles, t => Assert.Contains("c#", t, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("Kotlin day", titles);
    }

    [Fact]
    public async Task GetEvents_DateFilter_ReturnsEventsInRange()
    {
        // Arrange
        await SeedThreeEvents();
        var from = new DateTime(2026, 11, 5);
        var to = new DateTime(2026, 11, 30);

        // Act
        var result = await _service.GetEventsAsync(null, from, to, 1, 10);

        // Assert
        var single = Assert.Single(result.Events);
        Assert.Equal("Kotlin day", single.Title);
    }

    [Fact]
    public async Task GetEvents_Pagination_ReturnsRequestedPage()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
        {
            await CreateEvent($"Event {i}");
        }

        // Act
        var result = await _service.GetEventsAsync(null, null, null, 2, 2);

        // Assert
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.ItemsOnPage);
        Assert.Equal(["Event 3", "Event 4"], result.Events.Select(e => e.Title));
    }

    [Fact]
    public async Task GetEvents_CombinedFilters_ReturnsIntersection()
    {
        // Arrange
        await SeedThreeEvents();
        var from = new DateTime(2026, 11, 5);

        // Act
        var result = await _service.GetEventsAsync("c#", from, null, 1, 10);

        // Assert
        var single = Assert.Single(result.Events);
        Assert.Equal("Advanced C#", single.Title);
    }

    private Task<Event> CreateEvent(string title)
    {
        return _service.CreateEventAsync(
            title,
            null,
            new DateTime(2026, 11, 1, 10, 0, 0),
            new DateTime(2026, 11, 1, 12, 0, 0),
            10);
    }

    [Fact]
    public async Task GetEvent_NonExistingId_ThrowsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetEventAsync(id));
        Assert.Contains(id.ToString(), ex.Message);
    }

    [Fact]
    public async Task UpdateEvent_NonExistingId_ThrowsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var startAt = new DateTime(2026, 11, 1, 10, 0, 0);
        var endAt = new DateTime(2026, 11, 1, 12, 0, 0);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateEventAsync(id, "Title", null, startAt, endAt));
    }

    [Fact]
    public async Task CreateEvent_EndBeforeStart_ThrowsValidation()
    {
        // Arrange
        var startAt = new DateTime(2026, 11, 1, 12, 0, 0);
        var endAt = new DateTime(2026, 11, 1, 10, 0, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateEventAsync("Title", null, startAt, endAt, 10));
    }

    [Fact]
    public async Task UpdateEvent_EndBeforeStart_ThrowsValidation()
    {
        // Arrange
        var created = await CreateEvent("C# meetup");
        var startAt = new DateTime(2026, 11, 1, 12, 0, 0);
        var endAt = new DateTime(2026, 11, 1, 10, 0, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateEventAsync(created.Id, "C# meetup", null, startAt, endAt));
    }

    [Fact]
    public async Task GetEvents_InvalidPage_ThrowsValidation()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetEventsAsync(null, null, null, 0, 10));
    }

    [Fact]
    public async Task CreateEvent_EmptyTitle_ThrowsValidation()
    {
        // Arrange
        var startAt = new DateTime(2026, 11, 1, 10, 0, 0);
        var endAt = new DateTime(2026, 11, 1, 12, 0, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateEventAsync("   ", null, startAt, endAt, 10));
    }

    // новое
    [Fact]
    public async Task CreateEvent_ZeroSeats_ThrowsValidation()
    {
        // Arrange
        var startAt = new DateTime(2026, 11, 1, 10, 0, 0);
        var endAt = new DateTime(2026, 11, 1, 12, 0, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateEventAsync("Title", null, startAt, endAt, 0));
    }

    private Task<Event> CreateEvent(string title, DateTime startAt, DateTime endAt)
    {
        return _service.CreateEventAsync(title, null, startAt, endAt, 10);
    }

    private async Task SeedThreeEvents()
    {
        await CreateEvent("C# meetup", new DateTime(2026, 11, 1, 10, 0, 0), new DateTime(2026, 11, 1, 12, 0, 0));
        await CreateEvent("Kotlin day", new DateTime(2026, 11, 10, 10, 0, 0), new DateTime(2026, 11, 10, 18, 0, 0));
        await CreateEvent("Advanced C#", new DateTime(2026, 12, 5, 9, 0, 0), new DateTime(2026, 12, 6, 18, 0, 0));
    }
}