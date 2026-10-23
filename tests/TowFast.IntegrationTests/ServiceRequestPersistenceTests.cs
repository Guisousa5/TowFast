using Microsoft.EntityFrameworkCore;
using TowFast.Domain.ServiceRequests;
using TowFast.Infrastructure.Persistence;

namespace TowFast.IntegrationTests;

public sealed class ServiceRequestPersistenceTests
{
    [Fact]
    public async Task Reloaded_pending_request_can_be_cancelled_and_persists_new_history()
    {
        await using var db = CreateContext();
        var request = NewRequest();
        db.ServiceRequests.Add(request);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var reloaded = await db.ServiceRequests.Include(x => x.History).SingleAsync();
        reloaded.Cancel(Guid.NewGuid(), "Cancelado pelo cliente", DateTimeOffset.UtcNow);
        db.ChangeTracker.DetectChanges();

        Assert.Contains(db.ChangeTracker.Entries<ServiceRequestStatusHistory>(),
            entry => entry.State == EntityState.Added && entry.Entity.ToStatus == ServiceRequestStatus.Cancelled);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var persisted = await db.ServiceRequests.Include(x => x.History).SingleAsync();
        Assert.Equal(ServiceRequestStatus.Cancelled, persisted.Status);
        Assert.Equal(1, persisted.Version);
        Assert.Contains(persisted.History, x => x.ToStatus == ServiceRequestStatus.Cancelled);
    }

    [Fact]
    public async Task Reloaded_pending_request_can_be_accepted_and_persists_assignment()
    {
        await using var db = CreateContext();
        var request = NewRequest();
        db.ServiceRequests.Add(request);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var reloaded = await db.ServiceRequests.Include(x => x.History).Include(x => x.Assignments).SingleAsync();
        reloaded.Accept(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        db.ChangeTracker.DetectChanges();

        Assert.Contains(db.ChangeTracker.Entries<ServiceRequestStatusHistory>(), entry => entry.State == EntityState.Added);
        Assert.Contains(db.ChangeTracker.Entries<ServiceRequestAssignment>(), entry => entry.State == EntityState.Added);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var persisted = await db.ServiceRequests.Include(x => x.Assignments).SingleAsync();
        Assert.Equal(ServiceRequestStatus.Accepted, persisted.Status);
        Assert.Equal(1, persisted.Version);
        Assert.Single(persisted.Assignments);
    }

    private static TowFastDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TowFastDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TowFastDbContext(options);
    }

    private static ServiceRequest NewRequest() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, false,
        null, Guid.NewGuid(), DateTimeOffset.UtcNow);
}