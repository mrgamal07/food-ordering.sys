using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleRestaurantOrdering.Data;
using SingleRestaurantOrdering.Models;
using SingleRestaurantOrdering.ViewModels;

namespace SingleRestaurantOrdering.Controllers;

public class AuthController : Controller
{
    private readonly ApplicationDbContext _db;
    public AuthController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);
        var admin = await _db.Admins.SingleOrDefaultAsync(x => x.Email == model.Email);
        if (admin != null && new PasswordHasher<Admin>().VerifyHashedPassword(admin, admin.PasswordHash, model.Password) == PasswordVerificationResult.Success)
        {
            await SignInAsync(admin.AdminId.ToString(), admin.Email, admin.FullName, "Admin", model.RememberMe);
            return RedirectToAction("Index", "Admin");
        }
        var customer = await _db.Customers.SingleOrDefaultAsync(x => x.Email == model.Email);
        if (customer != null && new PasswordHasher<Customer>().VerifyHashedPassword(customer, customer.PasswordHash, model.Password) == PasswordVerificationResult.Success)
        {
            await SignInAsync(customer.CustomerId.ToString(), customer.Email, customer.FullName, "Customer", model.RememberMe);
            return Redirect(returnUrl is { Length: > 0 } && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home")!);
        }
        ModelState.AddModelError(string.Empty, "The email or password is incorrect.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (await _db.Customers.AnyAsync(x => x.Email == model.Email) || await _db.Admins.AnyAsync(x => x.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "An account already exists for this email.");
            return View(model);
        }
        var customer = new Customer { FullName = model.FullName, Email = model.Email, Phone = model.Phone, Address = model.Address };
        customer.PasswordHash = new PasswordHasher<Customer>().HashPassword(customer, model.Password);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Registration successful. Please sign in with your new email and password.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(string id, string email, string name, string role, bool persistent)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Email, email), new(ClaimTypes.Name, name), new(ClaimTypes.Role, role) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = persistent });
    }
}
