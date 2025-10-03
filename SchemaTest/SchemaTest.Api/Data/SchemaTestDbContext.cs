using Microsoft.EntityFrameworkCore;
using SchemaTest.Api.Models;

namespace SchemaTest.Api.Data;

public class SchemaTestDbContext(DbContextOptions<SchemaTestDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("schematest");

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(160);

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .ValueGeneratedOnAdd();
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var currentSchema = entityType.GetSchema();
            var tableName = entityType.GetTableName();

            if (string.IsNullOrEmpty(tableName))
            {
                continue;
            }

            // Skip tables that already have your convention applied
            if (!tableName.StartsWith("schematest_", StringComparison.OrdinalIgnoreCase))
            {
                entityType.SetTableName($"schematest_{tableName}");
            }

            // Optional: keep schema metadata if you still want it inside EF
            if (!string.IsNullOrEmpty(currentSchema))
            {
                entityType.SetSchema(currentSchema);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

}
