using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleRestaurantOrdering.Data;
using SingleRestaurantOrdering.Models;
using SingleRestaurantOrdering.ViewModels;

namespace SingleRestaurantOrdering.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var lowStock = _db.Inventory.Include(x => x.Food).Where(x => x.QuantityInStock <= x.ReorderLevel).OrderBy(x => x.QuantityInStock);
        return View(new AdminDashboardViewModel
        {
            TodayOrders = await _db.Orders.CountAsync(x => x.OrderDate >= today),
            TodayRevenue = await _db.Orders.Where(x => x.OrderDate >= today && x.PaymentStatus == "Paid").SumAsync(x => (decimal?)x.TotalAmount) ?? 0,
            TotalFoods = await _db.Foods.CountAsync(),
            LowStockItems = await lowStock.CountAsync(),
            LowStock = await lowStock.Take(5).ToListAsync(),
            RecentOrders = await _db.Orders.Include(x => x.Customer).OrderByDescending(x => x.OrderDate).Take(8).ToListAsync()
        });
    }

    public async Task<IActionResult> Foods()
    {
        ViewBag.Categories = await _db.FoodCategories.OrderBy(x => x.Name).ToListAsync();
        return View(await _db.Foods.Include(x => x.Category).Include(x => x.Inventory).OrderBy(x => x.Name).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFood(FoodEditViewModel model)
    {
        if (!ModelState.IsValid) { TempData["AdminError"] = "Please complete all food fields."; return RedirectToAction(nameof(Foods)); }
        var food = model.FoodId == 0 ? new Food() : await _db.Foods.Include(x => x.Inventory).SingleAsync(x => x.FoodId == model.FoodId);
        food.FoodCategoryId = model.FoodCategoryId; food.Name = model.Name; food.Description = model.Description; food.Price = model.Price; food.ImageUrl = model.ImageUrl; food.IsAvailable = model.IsAvailable;
        if (model.FoodId == 0) { _db.Foods.Add(food); await _db.SaveChangesAsync(); }
        var inventory = food.Inventory ?? new Inventory { FoodId = food.FoodId };
        inventory.QuantityInStock = model.StockQuantity; inventory.ReorderLevel = model.ReorderLevel; inventory.UpdatedAt = DateTime.UtcNow;
        if (inventory.InventoryId == 0) _db.Inventory.Add(inventory);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Foods));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFood(int id)
    {
        var food = await _db.Foods.FindAsync(id); if (food == null) return NotFound();
        food.IsAvailable = !food.IsAvailable; await _db.SaveChangesAsync(); return RedirectToAction(nameof(Foods));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFood(int id)
    {
        var food = await _db.Foods.FindAsync(id); if (food == null) return NotFound();
        food.IsAvailable = false; await _db.SaveChangesAsync(); return RedirectToAction(nameof(Foods));
    }

    public async Task<IActionResult> Categories() => View(await _db.FoodCategories.Include(x => x.Foods).OrderBy(x => x.Name).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(CategoryEditViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Categories));
        var category = model.FoodCategoryId == 0 ? new FoodCategory() : await _db.FoodCategories.FindAsync(model.FoodCategoryId) ?? new FoodCategory();
        category.Name = model.Name; category.Description = model.Description; category.IsActive = model.IsActive;
        if (model.FoodCategoryId == 0) _db.FoodCategories.Add(category);
        await _db.SaveChangesAsync(); return RedirectToAction(nameof(Categories));
    }

    public async Task<IActionResult> Orders() => View(await _db.Orders.Include(x => x.Customer).Include(x => x.Payment).OrderByDescending(x => x.OrderDate).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrder(int id, string status)
    {
        var order = await _db.Orders.FindAsync(id); if (order == null) return NotFound();
        order.Status = status; await _db.SaveChangesAsync(); return RedirectToAction(nameof(Orders));
    }

    public async Task<IActionResult> Inventory() => View(await _db.Inventory.Include(x => x.Food).OrderBy(x => x.QuantityInStock).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInventory(int id, int quantity, int reorderLevel)
    {
        var item = await _db.Inventory.FindAsync(id); if (item == null) return NotFound();
        item.QuantityInStock = Math.Max(0, quantity); item.ReorderLevel = Math.Max(0, reorderLevel); item.UpdatedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); return RedirectToAction(nameof(Inventory));
    }

    public async Task<IActionResult> SoldItems() => View(await _db.SoldItems.Include(x => x.Food).Include(x => x.Order).OrderByDescending(x => x.SoldAt).ToListAsync());
}
