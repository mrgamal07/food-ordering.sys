using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingleRestaurantOrdering.Models;

[Table("Customer")]
public class Customer
{
    [Key] public int CustomerId { get; set; }
    [Required, MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(160)] public string Email { get; set; } = string.Empty;
    [Required] public string PasswordHash { get; set; } = string.Empty;
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

[Table("Admin")]
public class Admin
{
    [Key] public int AdminId { get; set; }
    [Required, MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(160)] public string Email { get; set; } = string.Empty;
    [Required] public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("Food_Category")]
public class FoodCategory
{
    [Key] public int FoodCategoryId { get; set; }
    [Required, MaxLength(80)] public string Name { get; set; } = string.Empty;
    [MaxLength(300)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Food> Foods { get; set; } = new List<Food>();
}

[Table("Food")]
public class Food
{
    [Key] public int FoodId { get; set; }
    [Required] public int FoodCategoryId { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(600)] public string? Description { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Price { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public FoodCategory Category { get; set; } = null!;
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public Inventory? Inventory { get; set; }
    public ICollection<SoldItem> SoldItems { get; set; } = new List<SoldItem>();
}

[Table("Orders")]
public class Order
{
    [Key] public int OrderId { get; set; }
    [Required] public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(40)] public string Status { get; set; } = "Pending";
    [Column(TypeName = "decimal(10,2)")] public decimal TotalAmount { get; set; }
    [Required, MaxLength(300)] public string DeliveryAddress { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string PaymentStatus { get; set; } = "Unpaid";
    [MaxLength(30)] public string? PaymentMethod { get; set; }
    public Customer Customer { get; set; } = null!;
    public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
    public Payment? Payment { get; set; }
    public ICollection<SoldItem> SoldItems { get; set; } = new List<SoldItem>();
}

[Table("Order_Details")]
public class OrderDetail
{
    [Key] public int OrderDetailId { get; set; }
    [Required] public int OrderId { get; set; }
    [Required] public int FoodId { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal LineTotal { get; set; }
    public Order Order { get; set; } = null!;
    public Food Food { get; set; } = null!;
}

[Table("Payment")]
public class Payment
{
    [Key] public int PaymentId { get; set; }
    [Required] public int OrderId { get; set; }
    [MaxLength(160)] public string? TransactionId { get; set; }
    [Required, MaxLength(30)] public string PaymentMethod { get; set; } = string.Empty;
    [Column(TypeName = "decimal(10,2)")] public decimal Amount { get; set; }
    [Required, MaxLength(30)] public string Status { get; set; } = "Initiated";
    public DateTime? PaidAt { get; set; }
    [MaxLength(2000)] public string? GatewayResponse { get; set; }
    public Order Order { get; set; } = null!;
}

[Table("Inventory")]
public class Inventory
{
    [Key] public int InventoryId { get; set; }
    [Required] public int FoodId { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Food Food { get; set; } = null!;
}

[Table("Sold_Items")]
public class SoldItem
{
    [Key] public int SoldItemId { get; set; }
    [Required] public int OrderId { get; set; }
    [Required] public int FoodId { get; set; }
    public int Quantity { get; set; }
    public DateTime SoldAt { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(10,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal TotalAmount { get; set; }
    public Order Order { get; set; } = null!;
    public Food Food { get; set; } = null!;
}
