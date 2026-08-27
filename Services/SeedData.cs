using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SingleRestaurantOrdering.Data;
using SingleRestaurantOrdering.Models;

namespace SingleRestaurantOrdering.Services;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.Admins.AnyAsync())
        {
            var hasher = new PasswordHasher<Admin>();
            var admin = new Admin { FullName = "Restaurant Admin", Email = "admin@thaliandspice.com" };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");
            db.Admins.Add(admin);
        }

        if (!await db.FoodCategories.AnyAsync())
        {
            var categories = new[]
            {
                new FoodCategory { Name = "Signature Thali", Description = "Complete Nepali and North Indian platters." },
                new FoodCategory { Name = "Momo & Snacks", Description = "Hand-folded dumplings and crunchy favorites." },
                new FoodCategory { Name = "Curries & Rice", Description = "Slow-simmered comfort food made to order." },
                new FoodCategory { Name = "Drinks", Description = "Refreshing house drinks and chai." }
            };
            db.FoodCategories.AddRange(categories);
            await db.SaveChangesAsync();
        }

        if (!await db.Foods.AnyAsync())
        {
            var category = await db.FoodCategories.ToDictionaryAsync(x => x.Name);
            var foods = new[]
            {
                new Food { Name = "Thali & Spice Signature", Description = "Dal, seasonal tarkari, achar, rice, roti and a sweet finish.", Price = 650, FoodCategoryId = category["Signature Thali"].FoodCategoryId, ImageUrl = "/images/hero-food.jpg" },
                new Food { Name = "Jhol Momo", Description = "Steamed chicken momos in a warm sesame-tomato jhol.", Price = 320, FoodCategoryId = category["Momo & Snacks"].FoodCategoryId, ImageUrl = "/images/hero-food.jpg" },
                new Food { Name = "Kukhura Curry", Description = "Tender chicken in a fragrant tomato and ginger gravy.", Price = 480, FoodCategoryId = category["Curries & Rice"].FoodCategoryId, ImageUrl = "/images/hero-food.jpg" },
                new Food { Name = "Khasi Ko Sekuwa", Description = "Char-grilled goat skewers with smoky chili chutney.", Price = 540, FoodCategoryId = category["Momo & Snacks"].FoodCategoryId, ImageUrl = "/images/hero-food.jpg" },
                new Food { Name = "Butter Garlic Naan", Description = "Tandoor-baked naan brushed with garlic butter.", Price = 180, FoodCategoryId = category["Curries & Rice"].FoodCategoryId, ImageUrl = "/images/hero-food.jpg" },
                new Food { Name = "Masala Chai", Description = "Black tea simmered with cardamom, clove and cinnamon.", Price = 120, FoodCategoryId = category["Drinks"].FoodCategoryId, ImageUrl = "/images/hero-food.jpg" }
            };
            db.Foods.AddRange(foods);
            await db.SaveChangesAsync();
            db.Inventory.AddRange(foods.Select(food => new Inventory { FoodId = food.FoodId, QuantityInStock = 40, ReorderLevel = 10 }));
        }
        await db.SaveChangesAsync();
    }
}
