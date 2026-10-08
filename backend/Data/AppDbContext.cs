using AuthApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

            entity.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

            entity.Property(u => u.PassWordHash)
            .IsRequired();

            entity.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(50);

            entity.HasIndex(u => u.Email)
            .IsUnique();
        });
    }
}
