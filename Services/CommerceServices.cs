using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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
public record EsewaCallback(string TransactionCode, string Status, decimal TotalAmount, string TransactionUuid, string ProductCode, string Signature);
public record KhaltiInitiation(string Endpoint, string PublicKey, string Pidx, string OrderUrl);

public class PaymentGatewayService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    public PaymentGatewayService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public string CreateEsewaTransactionUuid(int orderId) => $"ORDER-{orderId}-{Guid.NewGuid():N}";

    public EsewaForm BuildEsewaForm(Order order, string callbackBaseUrl, string transactionUuid)
    {
        var config = _configuration.GetSection("Payments:eSewa");
        var secret = config["SecretKey"] ?? "8gBm/:&EnhH.1/q";
        var productCode = config["ProductCode"] ?? "EPAYTEST";
        var total = order.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture);
        var signedFields = "total_amount,transaction_uuid,product_code";
        var message = $"total_amount={total},transaction_uuid={transactionUuid},product_code={productCode}";
        var signature = CreateSignature(secret, message);
        var fields = new Dictionary<string, string>
        {
            ["amount"] = total, ["tax_amount"] = "0", ["total_amount"] = total,
            ["transaction_uuid"] = transactionUuid, ["product_code"] = productCode,
            ["product_service_charge"] = "0", ["product_delivery_charge"] = "0",
            ["success_url"] = $"{callbackBaseUrl}/Payment/Success?orderId={order.OrderId}",
            ["failure_url"] = $"{callbackBaseUrl}/Payment/Failure?orderId={order.OrderId}",
            ["signed_field_names"] = signedFields, ["signature"] = signature
        };
        return new EsewaForm(config["Endpoint"] ?? "https://rc-epay.esewa.com.np/api/epay/main/v2/form", fields);
    }

    public bool TryVerifyEsewaResponse(string encodedData, string expectedTransactionUuid, decimal expectedTotalAmount, out EsewaCallback? callback, out string error)
    {
        callback = null;
        error = string.Empty;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedData));
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var transactionUuid = root.GetProperty("transaction_uuid").GetString() ?? string.Empty;
            var productCode = root.GetProperty("product_code").GetString() ?? string.Empty;
            var status = root.GetProperty("status").GetString() ?? string.Empty;
            var transactionCode = root.TryGetProperty("transaction_code", out var code) ? code.GetString() ?? string.Empty : string.Empty;
            var returnedSignature = root.GetProperty("signature").GetString() ?? string.Empty;
            var signedFieldNames = root.GetProperty("signed_field_names").GetString() ?? string.Empty;
            var fields = signedFieldNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var message = string.Join(",", fields.Select(field => $"{field}={ReadField(root, field)}"));
            var expectedSignature = CreateSignature(_configuration["Payments:eSewa:SecretKey"] ?? "8gBm/:&EnhH.1/q", message);
            var amount = root.GetProperty("total_amount").GetDecimal();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedSignature), Encoding.UTF8.GetBytes(returnedSignature)))
            {
                error = "eSewa response signature validation failed.";
                return false;
            }
            if (!string.Equals(transactionUuid, expectedTransactionUuid, StringComparison.Ordinal))
            {
                error = "eSewa returned a different transaction UUID.";
                return false;
            }
            if (!string.Equals(productCode, _configuration["Payments:eSewa:ProductCode"] ?? "EPAYTEST", StringComparison.Ordinal))
            {
                error = "eSewa returned a different product code.";
                return false;
            }
            if (Math.Abs(amount - expectedTotalAmount) > 0.01m)
            {
                error = "eSewa returned a different total amount.";
                return false;
            }
            callback = new EsewaCallback(transactionCode, status, amount, transactionUuid, productCode, returnedSignature);
            return string.Equals(status, "COMPLETE", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or KeyNotFoundException)
        {
            error = "The eSewa callback data was invalid.";
            return false;
        }
    }

    private static string ReadField(JsonElement root, string field) => field switch
    {
        "signed_field_names" => root.GetProperty("signed_field_names").GetString() ?? string.Empty,
        _ => root.TryGetProperty(field, out var value) ? value.ToString() : string.Empty
    };

    private static string CreateSignature(string secret, string message)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
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
            website_url = _configuration["App:BaseUrl"] ?? "http://localhost:5062",
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
