using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<MeterReading> MeterReadings { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================== UNIQUE ==================
            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.Phone)
                .IsUnique();

            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.IdCard)
                .IsUnique();

            // ================== RELATION ==================
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Room)
                .WithMany()
                .HasForeignKey(c => c.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Tenant)
                .WithMany()
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Contract)
                .WithMany()
                .HasForeignKey(i => i.ContractId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany()
                .HasForeignKey(p => p.InvoiceId);

            // ================== FIX DECIMAL (QUAN TRỌNG) ==================

            modelBuilder.Entity<Service>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Room>()
                .Property(r => r.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Contract>()
                .Property(c => c.Deposit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.RoomFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.ElectricityFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.WaterFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.ServiceFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Total)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);
        }
    }
}