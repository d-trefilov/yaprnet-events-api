using EventsApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventsApi.DataAccess.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("events");
            builder.HasKey(evt => evt.Id);
            builder.Property(evt => evt.IsDeleted);
            builder.Property(evt => evt.Id).ValueGeneratedNever();
            builder.Property(evt => evt.Title).IsRequired().HasMaxLength(Event.MaxTitleLength);
            builder.Property(evt => evt.StartAt).HasColumnType("timestamp without time zone");
            builder.Property(evt => evt.EndAt).HasColumnType("timestamp without time zone");
        }
    }
}
