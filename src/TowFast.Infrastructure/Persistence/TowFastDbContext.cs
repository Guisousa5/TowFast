using Microsoft.EntityFrameworkCore;
using TowFast.Domain.Customers;
using TowFast.Domain.Identity;
using TowFast.Domain.Providers;
using TowFast.Domain.ServiceRequests;

namespace TowFast.Infrastructure.Persistence;

public sealed class TowFastDbContext(DbContextOptions<TowFastDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientVehicle> ClientVehicles => Set<ClientVehicle>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<ProviderVehicle> ProviderVehicles => Set<ProviderVehicle>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<ProviderServiceType> ProviderServiceTypes => Set<ProviderServiceType>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceRequestItem> ServiceRequestItems => Set<ServiceRequestItem>();
    public DbSet<ServiceRequestStatusHistory> ServiceRequestStatusHistory => Set<ServiceRequestStatusHistory>();
    public DbSet<ServiceRequestAssignment> ServiceRequestAssignments => Set<ServiceRequestAssignment>();
    public DbSet<ServiceRequestTracking> ServiceRequestTracking => Set<ServiceRequestTracking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureIdentity(modelBuilder);
        ConfigureCustomers(modelBuilder);
        ConfigureProviders(modelBuilder);
        ConfigureRequests(modelBuilder);
    }

    private static void ConfigureIdentity(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.ClientId).IsUnique().HasFilter("client_id IS NOT NULL");
            e.HasIndex(x => x.DriverId).IsUnique().HasFilter("driver_id IS NOT NULL");
        });
        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(128);
            e.Property(x => x.UpdatedAt).IsConcurrencyToken();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<RefreshToken>().WithMany().HasForeignKey(x => x.ReplacedByTokenId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCustomers(ModelBuilder b)
    {
        b.Entity<Client>(e =>
        {
            e.ToTable("clients");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Document).HasMaxLength(14);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.Phone).HasMaxLength(11);
            e.HasIndex(x => x.Document).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });
        b.Entity<ClientVehicle>(e =>
        {
            e.ToTable("client_vehicles");
            e.HasKey(x => x.Id);
            e.Property(x => x.LicensePlate).HasMaxLength(7);
            e.HasIndex(x => x.LicensePlate).IsUnique();
            e.HasOne<Client>().WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Location>(e =>
        {
            e.ToTable("locations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);
            e.HasOne<Client>().WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_locations_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
                t.HasCheckConstraint("ck_locations_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
            });
        });
    }

    private static void ConfigureProviders(ModelBuilder b)
    {
        b.Entity<Provider>(e =>
        {
            e.ToTable("providers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Document).HasMaxLength(14);
            e.HasIndex(x => x.Document).IsUnique();
            e.Property(x => x.ServiceCenterLatitude).HasPrecision(9, 6);
            e.Property(x => x.ServiceCenterLongitude).HasPrecision(9, 6);
            e.Property(x => x.ServiceRadiusKm).HasPrecision(9, 2);
            e.ToTable(t => t.HasCheckConstraint("ck_providers_radius", "service_radius_km > 0"));
        });
        b.Entity<Driver>(e =>
        {
            e.ToTable("drivers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Document).IsUnique();
            e.HasIndex(x => x.LicenseNumber).IsUnique();
            e.HasOne<Provider>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ProviderVehicle>(e =>
        {
            e.ToTable("provider_vehicles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.LicensePlate).IsUnique();
            e.Property(x => x.CapacityKg).HasPrecision(12, 2);
            e.HasOne<Provider>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ServiceType>(e =>
        {
            e.ToTable("service_types");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.BasePrice).HasPrecision(12, 2);
            e.Property(x => x.PricePerKm).HasPrecision(12, 2);
            e.HasIndex(x => x.Code).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_service_types_base_price", "base_price IS NULL OR base_price > 0");
                t.HasCheckConstraint("ck_service_types_price_per_km", "price_per_km IS NULL OR price_per_km > 0");
            });
            var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            e.HasData(
                new { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Code = ServiceTypeCode.Tow, Name = "Reboque", Description = "Remocao do veiculo", RequiresDestination = true, BasePrice = (decimal?)100m, PricePerKm = (decimal?)3m, IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt },
                new { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Code = ServiceTypeCode.BatteryReplacement, Name = "Troca de bateria", Description = (string?)null, RequiresDestination = false, BasePrice = (decimal?)150m, PricePerKm = (decimal?)null, IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt },
                new { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Code = ServiceTypeCode.TireChange, Name = "Troca de pneu", Description = (string?)null, RequiresDestination = false, BasePrice = (decimal?)80m, PricePerKm = (decimal?)null, IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt });
        });
        b.Entity<ProviderServiceType>(e =>
        {
            e.ToTable("provider_service_types");
            e.HasKey(x => new { x.ProviderId, x.ServiceTypeId });
            e.HasOne<Provider>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ServiceType>().WithMany().HasForeignKey(x => x.ServiceTypeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRequests(ModelBuilder b)
    {
        b.Entity<ServiceRequest>(e =>
        {
            e.ToTable("service_requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.PublicId).HasMaxLength(24);
            e.HasIndex(x => x.PublicId).IsUnique();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.QuotedPrice).HasPrecision(12, 2);
            e.Property(x => x.EstimatedDistanceKm).HasPrecision(10, 2);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_requests_quoted_price", "quoted_price IS NULL OR quoted_price > 0");
                t.HasCheckConstraint("ck_requests_distance", "estimated_distance_km IS NULL OR estimated_distance_km >= 0");
            });
            e.HasIndex(x => new { x.Status, x.CreatedAt, x.PublicId });
            e.HasIndex(x => x.ProviderId).IsUnique()
                .HasFilter("provider_id IS NOT NULL AND status IN ('Accepted', 'EnRoute', 'InProgress')");
            e.HasOne<Client>().WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ClientVehicle>().WithMany().HasForeignKey(x => x.ClientVehicleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ServiceType>().WithMany().HasForeignKey(x => x.ServiceTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Location>().WithMany().HasForeignKey(x => x.OriginLocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Location>().WithMany().HasForeignKey(x => x.DestinationLocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.History).WithOne().HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Assignments).WithOne().HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.History).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.Assignments).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        b.Entity<ServiceRequestItem>(e =>
        {
            e.ToTable("service_request_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Quantity).HasPrecision(12, 3);
            e.ToTable(t => t.HasCheckConstraint("ck_request_items_quantity", "quantity > 0"));
        });
        b.Entity<ServiceRequestStatusHistory>(e =>
        {
            e.ToTable("service_request_status_history");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.ServiceRequestId, x.CreatedAt });
        });
        b.Entity<ServiceRequestAssignment>(e =>
        {
            e.ToTable("service_request_assignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.ServiceRequestId, x.AcceptedAt });
        });

        b.Entity<ServiceRequestTracking>(e =>
        {
            e.ToTable("service_request_tracking");
            e.HasKey(x => x.ServiceRequestId);
            e.HasIndex(x => x.SourceEventId).IsUnique();
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);
            e.Property(x => x.AccuracyMeters).HasPrecision(10, 2);
            e.Property(x => x.SpeedKmh).HasPrecision(10, 2);
            e.Property(x => x.HeadingDegrees).HasPrecision(5, 2);
            e.HasOne<ServiceRequest>().WithOne().HasForeignKey<ServiceRequestTracking>(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        });
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static string ToSnakeCase(string value) => string.Concat(value.Select((character, index) =>
        index > 0 && char.IsUpper(character) ? $"_{char.ToLowerInvariant(character)}" : char.ToLowerInvariant(character).ToString()));
}
