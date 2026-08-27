using System.ComponentModel.DataAnnotations;
using SingleRestaurantOrdering.Models;

namespace SingleRestaurantOrdering.ViewModels;

public class RegisterViewModel
{
    [Required, Display(Name = "Full name"), MaxLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), MinLength(6)] public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password))] public string ConfirmPassword { get; set; } = string.Empty;
    [Phone] public string? Phone { get; set; }
    [Required, MaxLength(300)] public string Address { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class MenuViewModel
{
    public IEnumerable<FoodCategory> Categories { get; set; } = [];
    public IEnumerable<Food> Foods { get; set; } = [];
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
}

public class CartLineViewModel
{
    public Food Food { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal LineTotal => Food.Price * Quantity;
}

public class CartViewModel
{
    public List<CartLineViewModel> Lines { get; set; } = [];
    public decimal Total => Lines.Sum(x => x.LineTotal);
    public string DeliveryAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Cash on Delivery";
}

public class CheckoutViewModel
{
    [Required, MaxLength(300), Display(Name = "Delivery address")] public string DeliveryAddress { get; set; } = string.Empty;
    [Required] public string PaymentMethod { get; set; } = "Cash on Delivery";
}

public class AdminDashboardViewModel
{
    public int TodayOrders { get; set; }
    public decimal TodayRevenue { get; set; }
    public int TotalFoods { get; set; }
    public int LowStockItems { get; set; }
    public IEnumerable<Order> RecentOrders { get; set; } = [];
    public IEnumerable<Inventory> LowStock { get; set; } = [];
}

public class FoodEditViewModel
{
    public int FoodId { get; set; }
    [Required] public int FoodCategoryId { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(600)] public string? Description { get; set; }
    [Required, Range(0.01, 999999)] public decimal Price { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    [Range(0, 999999)] public int StockQuantity { get; set; }
    [Range(0, 999999)] public int ReorderLevel { get; set; } = 10;
}

public class CategoryEditViewModel
{
    public int FoodCategoryId { get; set; }
    [Required, MaxLength(80)] public string Name { get; set; } = string.Empty;
    [MaxLength(300)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
