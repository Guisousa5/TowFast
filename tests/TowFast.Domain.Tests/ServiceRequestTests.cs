using TowFast.Domain.Common;
using TowFast.Domain.ServiceRequests;

namespace TowFast.Domain.Tests;

public sealed class ServiceRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Tow_requires_destination()
    {
        Assert.Throws<DomainException>(() => new ServiceRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, true, null, Guid.NewGuid(), Now));
    }

    [Fact]
    public void Full_flow_records_status_history()
    {
        var userId = Guid.NewGuid();
        var request = NewRequest(userId);
        request.Accept(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), userId, Now.AddMinutes(1));
        request.MarkEnRoute(userId, Now.AddMinutes(2));
        request.Start(userId, Now.AddMinutes(3));
        request.Complete(userId, "Done", [], Now.AddMinutes(4));

        Assert.Equal(ServiceRequestStatus.Completed, request.Status);
        Assert.Equal(5, request.History.Count);
        Assert.Equal(4, request.Version);
        Assert.NotNull(request.CompletedAt);
    }

    [Fact]
    public void Release_returns_request_to_pending_and_clears_assignment()
    {
        var userId = Guid.NewGuid();
        var request = NewRequest(userId);
        request.Accept(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), userId, Now.AddMinutes(1));
        request.Release(userId, "Vehicle unavailable", Now.AddMinutes(2));

        Assert.Equal(ServiceRequestStatus.Pending, request.Status);
        Assert.Null(request.ProviderId);
        Assert.Equal(AssignmentStatus.Released, request.Assignments.Single().Status);
    }

    [Fact]
    public void Cannot_skip_state()
    {
        var request = NewRequest(Guid.NewGuid());
        Assert.Throws<DomainException>(() => request.Start(Guid.NewGuid(), Now));
    }

    private static ServiceRequest NewRequest(Guid userId) => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        Guid.NewGuid(), null, false, null, userId, Now);
}
