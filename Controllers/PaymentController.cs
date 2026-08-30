using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleRestaurantOrdering.Data;
using SingleRestaurantOrdering.Models;
using SingleRestaurantOrdering.Services;

namespace SingleRestaurantOrdering.Controllers;

[Authorize(Roles = "Customer")]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PaymentGatewayService _gateways;
    public PaymentController(ApplicationDbContext db, PaymentGatewayService gateways) { _db = db; _gateways = gateways; }

    public async Task<IActionResult> Start(int orderId)
    {
        var order = await _db.Orders.Include(x => x.Customer).SingleOrDefaultAsync(x => x.OrderId == orderId && x.CustomerId == CurrentCustomerId());
        if (order == null) return NotFound();
        if (order.PaymentStatus == "Paid") return RedirectToAction("Details", "Orders", new { id = orderId });

        if (order.PaymentMethod == "eSewa")
        {
            var payment = await _db.Payments.SingleOrDefaultAsync(x => x.OrderId == orderId);
            var transactionUuid = _gateways.CreateEsewaTransactionUuid(order.OrderId);
            if (payment == null)
            {
                payment = new Payment { OrderId = order.OrderId };
                _db.Payments.Add(payment);
            }
            payment.PaymentMethod = "eSewa";
            payment.Amount = order.TotalAmount;
            payment.Status = "Initiated";
            payment.TransactionId = transactionUuid;
            payment.PaidAt = null;
            payment.GatewayResponse = null;
            await _db.SaveChangesAsync();
            return View("Esewa", _gateways.BuildEsewaForm(order, BaseUrl(), transactionUuid));
        }

        if (order.PaymentMethod == "Khalti")
        {
            var url = $"{BaseUrl()}/Payment/KhaltiCallback?orderId={order.OrderId}";
            var initiation = await _gateways.InitiateKhaltiAsync(order, url);
            if (initiation != null) return Redirect($"{initiation.Endpoint}login?pidx={initiation.Pidx}");
            TempData["PaymentError"] = "Khalti is not configured yet. Add your secret key in appsettings or environment variables.";
        }
        return RedirectToAction("Details", "Orders", new { id = orderId });
    }

    [AllowAnonymous]
    [HttpGet("Payment/Success/{orderId:int}")]
    public async Task<IActionResult> Success(int orderId, string? data)
    {
        var order = await _db.Orders.Include(x => x.Details).SingleOrDefaultAsync(x => x.OrderId == orderId);
        if (order == null) return NotFound();
        var payment = await _db.Payments.SingleOrDefaultAsync(x => x.OrderId == orderId);
        if (payment == null || string.IsNullOrWhiteSpace(payment.TransactionId) || string.IsNullOrWhiteSpace(data))
        {
            TempData["PaymentError"] = "The eSewa payment response could not be verified.";
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }

        if (!_gateways.TryVerifyEsewaResponse(data, payment.TransactionId, order.TotalAmount, out var callback, out var error))
        {
            TempData["PaymentError"] = error;
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }

        order.PaymentStatus = "Paid";
        order.Status = "Confirmed";
        payment.PaymentMethod = "eSewa";
        payment.Amount = order.TotalAmount;
        payment.Status = "Completed";
        payment.PaidAt = DateTime.UtcNow;
        payment.TransactionId = callback!.TransactionUuid;
        payment.GatewayResponse = data;
        await RecordSoldItemsAsync(order);
        await _db.SaveChangesAsync();
        return RedirectToAction("Details", "Orders", new { id = orderId });
    }

    [AllowAnonymous]
    [HttpGet("Payment/Failure/{orderId:int}")]
    public async Task<IActionResult> Failure(int orderId)
    {
        var order = await _db.Orders.SingleOrDefaultAsync(x => x.OrderId == orderId);
        if (order == null) return NotFound();
        if (order.PaymentStatus != "Paid") order.PaymentStatus = "Failed";
        await _db.SaveChangesAsync();
        return RedirectToAction("Details", "Orders", new { id = orderId });
    }

    [AllowAnonymous]
    public async Task<IActionResult> KhaltiCallback(int orderId, string? pidx, string? status)
    {
        var order = await _db.Orders.Include(x => x.Details).SingleOrDefaultAsync(x => x.OrderId == orderId);
        if (order == null) return NotFound();
        var verified = await _gateways.VerifyKhaltiAsync(pidx ?? string.Empty);
        if (verified)
        {
            order.PaymentStatus = "Paid"; order.Status = "Confirmed";
            var payment = await _db.Payments.SingleOrDefaultAsync(x => x.OrderId == orderId) ?? new Payment { OrderId = orderId };
            payment.PaymentMethod = "Khalti"; payment.Amount = order.TotalAmount; payment.Status = "Completed"; payment.PaidAt = DateTime.UtcNow; payment.TransactionId = pidx;
            if (payment.PaymentId == 0) _db.Payments.Add(payment);
            await RecordSoldItemsAsync(order);
            await _db.SaveChangesAsync();
        }
        else
        {
            TempData["PaymentError"] = "Khalti could not verify this payment.";
        }
        return RedirectToAction("Details", "Orders", new { id = orderId });
    }

    private async Task RecordSoldItemsAsync(Order order)
    {
        if (await _db.SoldItems.AnyAsync(x => x.OrderId == order.OrderId)) return;
        foreach (var detail in order.Details) _db.SoldItems.Add(new SoldItem { OrderId = order.OrderId, FoodId = detail.FoodId, Quantity = detail.Quantity, UnitPrice = detail.UnitPrice, TotalAmount = detail.LineTotal });
    }
    private int CurrentCustomerId() => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private string BaseUrl() => HttpContext.RequestServices.GetRequiredService<IConfiguration>()["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
}
