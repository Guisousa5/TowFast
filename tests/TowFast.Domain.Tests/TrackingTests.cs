using TowFast.Domain.ServiceRequests;

namespace TowFast.Domain.Tests;

public sealed class TrackingTests
{
    [Fact]
    public void Older_or_duplicate_position_does_not_replace_latest()
    {
        var now = DateTimeOffset.UtcNow;
        var eventId = Guid.NewGuid();
        var tracking = new ServiceRequestTracking(Guid.NewGuid(), eventId, Guid.NewGuid(), Guid.NewGuid(),
            -23.5m, -46.6m, now, now, 5m, 20m, 90m);

        var duplicate = tracking.TryUpdate(eventId, Guid.NewGuid(), Guid.NewGuid(), -22m, -45m,
            now.AddSeconds(1), now.AddSeconds(1), null, null, null);
        var older = tracking.TryUpdate(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -22m, -45m,
            now.AddSeconds(-1), now.AddSeconds(1), null, null, null);

        Assert.False(duplicate);
        Assert.False(older);
        Assert.Equal(-23.5m, tracking.Latitude);
    }

    [Fact]
    public void Newer_position_replaces_current()
    {
        var now = DateTimeOffset.UtcNow;
        var tracking = new ServiceRequestTracking(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            -23.5m, -46.6m, now, now, null, null, null);

        var updated = tracking.TryUpdate(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -23.6m, -46.7m,
            now.AddSeconds(10), now.AddSeconds(11), 3m, 10m, 180m);

        Assert.True(updated);
        Assert.Equal(-23.6m, tracking.Latitude);
    }
}
