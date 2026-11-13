using EventsApi.Models;

namespace EventsApi.Tests;

public class BookingTests
{
    [Fact]
    public void CreatePending_NewBooking_HasPendingStatusAndNoProcessedAt()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var booking = Booking.CreatePending(eventId);

        // Assert
        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventId, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    public void Confirm_PendingBooking_SetsConfirmedAndProcessedAt()
    {
        // Arrange
        var booking = Booking.CreatePending(Guid.NewGuid());

        // Act
        booking.Confirm();

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void Reject_PendingBooking_SetsRejectedAndProcessedAt()
    {
        // Arrange
        var booking = Booking.CreatePending(Guid.NewGuid());

        // Act
        booking.Reject();

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void Confirm_AlreadyConfirmedBooking_ThrowsInvalidOperation()
    {
        // Arrange
        var booking = Booking.CreatePending(Guid.NewGuid());
        booking.Confirm();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => booking.Confirm());
    }
}