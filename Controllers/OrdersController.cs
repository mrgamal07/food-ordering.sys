using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleRestaurantOrdering.Data;

namespace SingleRestaurantOrdering.Controllers;

[Authorize(Roles = "Customer")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    public OrdersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await _db.Orders.Where(x => x.CustomerId == customerId).Include(x => x.Details).OrderByDescending(x => x.OrderDate).ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.Include(x => x.Details).ThenInclude(x => x.Food).Include(x => x.Payment).SingleOrDefaultAsync(x => x.OrderId == id && x.CustomerId == customerId);
        return order == null ? NotFound() : View(order);
    }
}
