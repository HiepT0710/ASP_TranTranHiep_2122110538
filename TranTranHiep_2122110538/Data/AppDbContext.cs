using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<OrderPayment> OrderPayments => Set<OrderPayment>();
    public DbSet<FoodReview> FoodReviews => Set<FoodReview>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<OrderMessage> OrderMessages => Set<OrderMessage>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Restaurant>()
            .HasIndex(r => r.OwnerId)
            .IsUnique();

        modelBuilder.Entity<Restaurant>()
            .HasOne(r => r.Owner)
            .WithOne(u => u.OwnedRestaurant)
            .HasForeignKey<Restaurant>(r => r.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.Restaurant)
            .WithMany(r => r.Categories)
            .HasForeignKey(c => c.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Food>()
            .HasOne(f => f.Restaurant)
            .WithMany(r => r.Foods)
            .HasForeignKey(f => f.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Food>()
            .HasOne(f => f.Category)
            .WithMany(c => c.Foods)
            .HasForeignKey(f => f.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Restaurant)
            .WithMany(r => r.Orders)
            .HasForeignKey(o => o.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Food)
            .WithMany(f => f.OrderDetails)
            .HasForeignKey(od => od.FoodId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Food>()
            .Property(f => f.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderDetail>()
            .Property(od => od.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne(h => h.Order)
            .WithMany(o => o.StatusHistories)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne(h => h.Actor)
            .WithMany(u => u.OrderStatusHistoriesAsActor)
            .HasForeignKey(h => h.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderStatusHistory>()
            .HasIndex(h => h.OrderId);

        modelBuilder.Entity<OrderPayment>()
            .HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderPayment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<FoodReview>()
            .HasOne(r => r.Order)
            .WithMany(o => o.FoodReviews)
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FoodReview>()
            .HasOne(r => r.Food)
            .WithMany(f => f.Reviews)
            .HasForeignKey(r => r.FoodId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FoodReview>()
            .HasOne(r => r.User)
            .WithMany(u => u.FoodReviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FoodReview>()
            .HasIndex(r => new { r.OrderId, r.FoodId })
            .IsUnique();

        modelBuilder.Entity<CartItem>()
            .HasOne(c => c.User)
            .WithMany(u => u.CartItems)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .HasOne(c => c.Food)
            .WithMany()
            .HasForeignKey(c => c.FoodId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CartItem>()
            .HasIndex(c => new { c.UserId, c.FoodId })
            .IsUnique();

        modelBuilder.Entity<OrderMessage>()
            .HasOne(m => m.Order)
            .WithMany(o => o.OrderMessages)
            .HasForeignKey(m => m.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderMessage>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderMessage>()
            .HasIndex(m => m.OrderId);

        modelBuilder.Entity<PushSubscription>()
            .HasOne(s => s.User)
            .WithMany(u => u.PushSubscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PushSubscription>()
            .HasIndex(s => s.Endpoint)
            .IsUnique();
    }
}
