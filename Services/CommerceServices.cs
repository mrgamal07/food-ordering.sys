using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SingleRestaurantOrdering.Data;
using SingleRestaurantOrdering.Models;

namespace SingleRestaurantOrdering.Services;

public static class CartSession
{
    private const string Key = "restaurant-cart";
    public static Dictionary<int, int> Get(HttpContext context) => context.Session.GetString(Key) is { } raw
        ? JsonSerializer.Deserialize<Dictionary<int, int>>(raw) ?? []
        : [];
    public static void Save(HttpContext context, Dictionary<int, int> cart) => context.Session.SetString(Key, JsonSerializer.Serialize(cart));
    public static void Clear(HttpContext context) => context.Session.Remove(Key);
}

public record EsewaForm(string Action, Dictionary<string, string> Fields);
public record KhaltiInitiation(string Endpoint, string PublicKey, string Pidx, string OrderUrl);

public class PaymentGatewayService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _db;
    public PaymentGatewayService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ApplicationDbContext db)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _db = db;
    }

    public EsewaForm BuildEsewaForm(Order order, string callbackBaseUrl)
    {
        var config = _configuration.GetSection("Payments:eSewa");
        var secret = config["SecretKey"] ?? "8gBm/:&EnhH.1/q";
        var productCode = config["ProductCode"] ?? "EPAYTEST";
        var total = order.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture);
        var signedFields = "total_amount,transaction_uuid,product_code";
        var message = $"total_amount={total},transaction_uuid=ORDER-{order.OrderId},product_code={productCode}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
        var fields = new Dictionary<string, string>
        {
            ["amount"] = total, ["tax_amount"] = "0", ["total_amount"] = total,
            ["transaction_uuid"] = $"ORDER-{order.OrderId}", ["product_code"] = productCode,
            ["product_service_charge"] = "0", ["product_delivery_charge"] = "0",
            ["success_url"] = $"{callbackBaseUrl}/Payment/Success?orderId={order.OrderId}",
            ["failure_url"] = $"{callbackBaseUrl}/Payment/Failure?orderId={order.OrderId}",
            ["signed_field_names"] = signedFields, ["signature"] = signature
        };
        return new EsewaForm(config["Endpoint"] ?? "https://rc-epay.esewa.com.np/api/epay/main/v2/form", fields);
    }

    public async Task<KhaltiInitiation?> InitiateKhaltiAsync(Order order, string returnUrl)
    {
        var config = _configuration.GetSection("Payments:Khalti");
        var secretKey = config["SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey)) return null;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Key", secretKey);
        var payload = new
        {
            return_url = returnUrl,
            website_url = _configuration["App:BaseUrl"] ?? "https://localhost:5001",
            amount = (int)(order.TotalAmount * 100),
            purchase_order_id = $"ORDER-{order.OrderId}",
            purchase_order_name = "Thali & Spice order",
            customer_info = new { name = order.Customer.FullName, email = order.Customer.Email }
        };
        var response = await client.PostAsJsonAsync(config["InitiateEndpoint"] ?? "https://a.khalti.com/api/v2/epayment/initiate/", payload);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<KhaltiResponse>();
        return result is null ? null : new KhaltiInitiation(config["CheckoutEndpoint"] ?? "https://a.khalti.com/api/v2/epayment/", config["PublicKey"] ?? "", result.pidx, returnUrl);
    }

    public async Task<bool> VerifyKhaltiAsync(string pidx)
    {
        var config = _configuration.GetSection("Payments:Khalti");
        var secretKey = config["SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(pidx)) return false;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Key", secretKey);
        var response = await client.PostAsJsonAsync(config["LookupEndpoint"] ?? "https://a.khalti.com/api/v2/epayment/lookup/", new { pidx });
        if (!response.IsSuccessStatusCode) return false;
        var result = await response.Content.ReadFromJsonAsync<KhaltiLookupResponse>();
        return string.Equals(result?.status, "Completed", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class KhaltiResponse { public string pidx { get; set; } = string.Empty; }
    private sealed class KhaltiLookupResponse { public string status { get; set; } = string.Empty; }
}
