using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AccessControl.Infrastructure.Data;

public class AccessControlDbContext : DbContext
{
    public AccessControlDbContext(DbContextOptions<AccessControlDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<EmployeeDepartment> EmployeeDepartments => Set<EmployeeDepartment>();
    public DbSet<DepartmentAccessPoint> DepartmentAccessPoints => Set<DepartmentAccessPoint>();
    public DbSet<NfcCard> NfcCards => Set<NfcCard>();
    public DbSet<AccessPoint> AccessPoints => Set<AccessPoint>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<AccessRule> AccessRules => Set<AccessRule>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<AccessEvent> AccessEvents => Set<AccessEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.FullName).HasColumnName("FullName").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");
            entity.Property(e => e.FaceImage).HasColumnName("FaceImage");
            entity.Property(e => e.FaceEmbedding).HasColumnName("FaceEmbedding");
            entity.Property(e => e.BiometricUpdatedAt).HasColumnName("BiometricUpdatedAt").HasColumnType("timestamp without time zone");
            entity.Ignore(e => e.DepartmentNames);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).HasColumnName("Name").IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<EmployeeDepartment>(entity =>
        {
            entity.ToTable("EmployeeDepartments");
            entity.HasKey(e => new { e.EmployeeId, e.DepartmentId });
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeId");
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentId");
            entity.HasOne(e => e.Employee)
                .WithMany(e => e.EmployeeDepartments)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Department)
                .WithMany(d => d.EmployeeDepartments)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DepartmentAccessPoint>(entity =>
        {
            entity.ToTable("DepartmentAccessPoints");
            entity.HasKey(e => new { e.DepartmentId, e.AccessPointId });
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentId");
            entity.Property(e => e.AccessPointId).HasColumnName("AccessPointId");
            entity.HasOne(e => e.Department)
                .WithMany(d => d.DepartmentAccessPoints)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AccessPoint)
                .WithMany(p => p.DepartmentAccessPoints)
                .HasForeignKey(e => e.AccessPointId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NfcCard>(entity =>
        {
            entity.ToTable("NfcCards");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Uid).HasColumnName("Uid").IsRequired();
            entity.HasIndex(e => e.Uid).IsUnique();
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeId");
            entity.Property(e => e.CardType).HasColumnName("CardType").HasConversion<string>().HasDefaultValue(CardType.Employee);
            entity.Property(e => e.IssuedAt).HasColumnName("IssuedAt").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");
            entity.Property(e => e.ExpiresAt).HasColumnName("ExpiresAt").HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
            entity.HasOne(e => e.Employee)
                .WithMany(e => e.Cards)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessPoint>(entity =>
        {
            entity.ToTable("AccessPoints");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).HasColumnName("Name").IsRequired();
            entity.Property(e => e.Location).HasColumnName("Location");
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
            entity.Property(e => e.IsGuestAccess).HasColumnName("IsGuestAccess").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");
            entity.Ignore(e => e.DepartmentNames);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.ToTable("Schedules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).HasColumnName("Name").IsRequired();
            entity.Property(e => e.ScheduleJson).HasColumnName("ScheduleJson").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<AccessRule>(entity =>
        {
            entity.ToTable("AccessRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeId");
            entity.Property(e => e.AccessPointId).HasColumnName("AccessPointId");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleId");
            entity.Property(e => e.ValidFrom).HasColumnName("ValidFrom").HasColumnType("timestamp without time zone");
            entity.Property(e => e.ValidTo).HasColumnName("ValidTo").HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AccessPoint)
                .WithMany(p => p.AccessRules)
                .HasForeignKey(e => e.AccessPointId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Schedule)
                .WithMany()
                .HasForeignKey(e => e.ScheduleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("Devices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).HasColumnName("Name").IsRequired();
            entity.Property(e => e.Location).HasColumnName("Location");
            entity.Property(e => e.AccessPointId).HasColumnName("AccessPointId");
            entity.HasOne(e => e.AccessPoint)
                .WithMany()
                .HasForeignKey(e => e.AccessPointId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<AccessEvent>(entity =>
        {
            entity.ToTable("AccessEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.DeviceId).HasColumnName("DeviceId");
            entity.Property(e => e.AccessPointId).HasColumnName("AccessPointId");
            entity.Property(e => e.CardUid).HasColumnName("CardUid");
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeId");
            entity.Property(e => e.EventTime).HasColumnName("EventTime").HasColumnType("timestamp without time zone").IsRequired();
            entity.Property(e => e.AccessGranted).HasColumnName("AccessGranted").IsRequired();
            entity.Property(e => e.Reason).HasColumnName("Reason").HasConversion<string>();
        });

        ApplyUtcToUnspecifiedDateTimeConverter(modelBuilder);
    }

    private static void ApplyUtcToUnspecifiedDateTimeConverter(ModelBuilder modelBuilder)
    {
        var converter = new ValueConverter<DateTime, DateTime>(
            v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
            v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified));

        var nullableConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(converter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableConverter);
                }
            }
        }
    }
}
