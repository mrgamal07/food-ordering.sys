using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleRestaurantOrdering.Data;
using SingleRestaurantOrdering.Models;
using SingleRestaurantOrdering.Services;
using SingleRestaurantOrdering.ViewModels;

namespace SingleRestaurantOrdering.Controllers;

public class CartController : Controller
{
    private readonly ApplicationDbContext _db;
    public CartController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var cart = CartSession.Get(HttpContext);
        var foods = await _db.Foods.Where(x => cart.Keys.Contains(x.FoodId)).ToListAsync();
        return View(new CartViewModel { Lines = foods.Select(food => new CartLineViewModel { Food = food, Quantity = cart[food.FoodId] }).ToList() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int foodId, int quantity = 1)
    {
        var food = await _db.Foods.SingleOrDefaultAsync(x => x.FoodId == foodId && x.IsAvailable);
        if (food == null) return NotFound();
        var cart = CartSession.Get(HttpContext);
        cart[foodId] = Math.Clamp(cart.GetValueOrDefault(foodId) + quantity, 1, 99);
        CartSession.Save(HttpContext, cart);
        TempData["Toast"] = $"{food.Name} added to your table.";
        return Redirect(Request.Headers.Referer.ToString().Length > 0 ? Request.Headers.Referer.ToString() : Url.Action("Index", "Home")!);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Update(int foodId, int quantity)
    {
        var cart = CartSession.Get(HttpContext);
        if (quantity <= 0) cart.Remove(foodId); else cart[foodId] = Math.Clamp(quantity, 1, 99);
        CartSession.Save(HttpContext, cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int foodId)
    {
        var cart = CartSession.Get(HttpContext); cart.Remove(foodId); CartSession.Save(HttpContext, cart);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Customer")]
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        if (!CartSession.Get(HttpContext).Any()) return RedirectToAction(nameof(Index));
        var customer = await CurrentCustomerAsync();
        return View(new CheckoutViewModel { DeliveryAddress = customer?.Address ?? string.Empty });
    }

    [Authorize(Roles = "Customer")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var cart = CartSession.Get(HttpContext);
        if (!cart.Any()) ModelState.AddModelError(string.Empty, "Your cart is empty.");
        if (!ModelState.IsValid) return View(model);
        var foods = await _db.Foods.Include(x => x.Inventory).Where(x => cart.Keys.Contains(x.FoodId) && x.IsAvailable).ToListAsync();
        if (foods.Count != cart.Count || foods.Any(x => x.Inventory == null || x.Inventory.QuantityInStock < cart[x.FoodId]))
        {
            ModelState.AddModelError(string.Empty, "One or more items are no longer available in the requested quantity.");
            return View(model);
        }
        var order = new Order { CustomerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), DeliveryAddress = model.DeliveryAddress, PaymentMethod = model.PaymentMethod, Status = "Preparing", PaymentStatus = model.PaymentMethod == "Cash on Delivery" ? "Pending" : "Initiated" };
        foreach (var food in foods)
        {
            var quantity = cart[food.FoodId];
            order.Details.Add(new OrderDetail { FoodId = food.FoodId, Quantity = quantity, UnitPrice = food.Price, LineTotal = food.Price * quantity });
            order.TotalAmount += food.Price * quantity;
            food.Inventory!.QuantityInStock -= quantity;
        }
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        CartSession.Clear(HttpContext);
        if (model.PaymentMethod == "Cash on Delivery") return RedirectToAction("Details", "Orders", new { id = order.OrderId });
        return RedirectToAction("Start", "Payment", new { orderId = order.OrderId });
    }

    private Task<Customer?> CurrentCustomerAsync() => _db.Customers.SingleOrDefaultAsync(x => x.CustomerId == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
}
