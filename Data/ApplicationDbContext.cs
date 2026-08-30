using Microsoft.EntityFrameworkCore;
using SingleRestaurantOrdering.Models;

namespace SingleRestaurantOrdering.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<FoodCategory> FoodCategories => Set<FoodCategory>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Inventory> Inventory => Set<Inventory>();
    public DbSet<SoldItem> SoldItems => Set<SoldItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Customer>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Admin>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(x => x.OrderId).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(x => x.TransactionId).IsUnique();
        modelBuilder.Entity<Food>().Property(x => x.Price).HasPrecision(10, 2);
        modelBuilder.Entity<Order>().Property(x => x.TotalAmount).HasPrecision(10, 2);
        modelBuilder.Entity<OrderDetail>().Property(x => x.UnitPrice).HasPrecision(10, 2);
        modelBuilder.Entity<OrderDetail>().Property(x => x.LineTotal).HasPrecision(10, 2);
        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(10, 2);
        modelBuilder.Entity<SoldItem>().Property(x => x.UnitPrice).HasPrecision(10, 2);
        modelBuilder.Entity<SoldItem>().Property(x => x.TotalAmount).HasPrecision(10, 2);

        modelBuilder.Entity<FoodCategory>().HasMany(x => x.Foods).WithOne(x => x.Category)
            .HasForeignKey(x => x.FoodCategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Customer>().HasMany(x => x.Orders).WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasMany(x => x.Details).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Food>().HasMany(x => x.OrderDetails).WithOne(x => x.Food)
            .HasForeignKey(x => x.FoodId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasOne(x => x.Payment).WithOne(x => x.Order)
            .HasForeignKey<Payment>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Food>().HasOne(x => x.Inventory).WithOne(x => x.Food)
            .HasForeignKey<Inventory>(x => x.FoodId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Order>().HasMany(x => x.SoldItems).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Food>().HasMany(x => x.SoldItems).WithOne(x => x.Food)
            .HasForeignKey(x => x.FoodId).OnDelete(DeleteBehavior.Restrict);
    }
}
