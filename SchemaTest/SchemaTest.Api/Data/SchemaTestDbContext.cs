using Microsoft.EntityFrameworkCore;
using SchemaTest.Api.Models;

namespace SchemaTest.Api.Data;

public class SchemaTestDbContext(DbContextOptions<SchemaTestDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers", "schematest");

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
    }
}
