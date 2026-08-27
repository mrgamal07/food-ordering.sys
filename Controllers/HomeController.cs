using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleRestaurantOrdering.Data;
using SingleRestaurantOrdering.Models;
using SingleRestaurantOrdering.ViewModels;

namespace SingleRestaurantOrdering.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? categoryId, string? search)
    {
        var foods = _db.Foods.Include(x => x.Category).Where(x => x.IsAvailable).AsQueryable();
        if (categoryId.HasValue) foods = foods.Where(x => x.FoodCategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(search)) foods = foods.Where(x => x.Name.Contains(search) || (x.Description ?? "").Contains(search));
        return View(new MenuViewModel { Categories = await _db.FoodCategories.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), Foods = await foods.OrderBy(x => x.Name).ToListAsync(), CategoryId = categoryId, Search = search });
    }

    public async Task<IActionResult> Menu(int? categoryId, string? search) => await Index(categoryId, search);

    public async Task<IActionResult> Food(int id)
    {
        var food = await _db.Foods.Include(x => x.Category).SingleOrDefaultAsync(x => x.FoodId == id && x.IsAvailable);
        return food == null ? NotFound() : View(food);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
