using EventsApi.DataAccess;
using EventsApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventsApi.Tests;

internal static class TestServices
{
    public static ServiceProvider Create()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        return services.BuildServiceProvider(validateScopes: true);
    }
}
