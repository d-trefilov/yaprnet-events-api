using EventsApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventsApi.DataAccess.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("bookings");
            builder.HasKey(booking => booking.Id);
            builder.Property(booking => booking.Id).ValueGeneratedNever();
            builder.Property(booking => booking.Status).HasConversion<string>();
            builder.HasOne(booking => booking.Event)
                .WithMany(evt => evt.Bookings)
                .HasForeignKey(booking => booking.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
