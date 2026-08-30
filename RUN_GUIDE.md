# Thali & Spice — Step-by-Step Run Guide

## 1. Install the required software

Install the following software on your computer:

| Software | Recommended version | Purpose |
| --- | --- | --- |
| .NET SDK | 8.0 or newer | Build and run the ASP.NET Core MVC application |
| Git | Current version | Download the project from GitHub |
| MySQL Server | 8.0 or newer | Optional persistent database |
| MySQL Workbench | Current version | Optional graphical database management |

Check that .NET and Git are available:

```bash
dotnet --version
git --version
```

The application was built for **.NET 8**. If `dotnet` is not recognized, install the .NET 8 SDK from the official Microsoft .NET download page.

## 2. Download the project

Open PowerShell, Command Prompt, Terminal, or Git Bash and run:

```bash
git clone https://github.com/mrgamal07/food-ordering.sys.git
cd food-ordering.sys
```

If you download the ZIP file instead, extract it and open the terminal inside the extracted `SingleRestaurantOrdering` folder.

## 3. Configure MySQL on port 3307

The project is configured for a local MySQL server at `localhost:3307`, using the database name `thali_spice` and the `root` user. Open `appsettings.json` and replace `YOUR_MYSQL_PASSWORD` with your actual MySQL password:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3307;Database=thali_spice;User=root;Password=YOUR_MYSQL_PASSWORD;AllowPublicKeyRetrieval=True;SslMode=None;"
}
```

Run the SQL from `Database/schema.sql` in MySQL Workbench first, or let EF Core create missing tables when the application starts. Then restore dependencies and start the application:

```bash
dotnet restore
dotnet build
dotnet run
```

The terminal will print one or more URLs, such as:

```text
Now listening on: https://localhost:7123
Now listening on: http://localhost:5123
```

Open the HTTPS URL in your browser. If the browser displays a development-certificate warning, use the HTTP URL or run:

```bash
dotnet dev-certs https --trust
```

With MySQL configured, customer accounts, orders, inventory, payments, and sold-item records remain available after the application restarts.

## 4. Test the customer workflow

Open `/Auth/Register` or click **Sign in → Create an account**. Register a customer with a name, email, password, phone, and delivery address.

Then follow this flow:

1. Open the menu on the home page.
2. Filter by category or search for a dish.
3. Click **Add to order**.
4. Open **Cart** in the top navigation.
5. Change quantities if needed.
6. Click **Continue to checkout**.
7. Enter or confirm the delivery address.
8. Select **Cash on delivery** for the easiest local test.
9. Click **Place order**.
10. Open **My orders** to view the order and tracking timeline.

## 5. Test the admin dashboard

Use the seeded development admin account:

| Field | Value |
| --- | --- |
| Email | `admin@thaliandspice.com` |
| Password | `Admin@123` |

Sign out from the customer account if necessary, then sign in with the admin account. Open `/Admin` or click **Dashboard** in the navigation.

The admin area includes:

- Overview KPIs for orders, revenue, menu items, and low stock.
- Menu item management at `/Admin/Foods`.
- Category management at `/Admin/Categories`.
- Order status management at `/Admin/Orders`.
- Inventory updates at `/Admin/Inventory`.
- Sold-item reporting at `/Admin/SoldItems`.

## 6. Run with MySQL Workbench

Start MySQL Server on port `3307` and create the database. You can run the included script from a MySQL client:

```bash
mysql -u root -p < Database/schema.sql
```

Alternatively, create the database manually:

```sql
CREATE DATABASE thali_spice CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

Edit `appsettings.json` and replace the empty connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3307;Database=thali_spice;User=root;Password=YOUR_MYSQL_PASSWORD;AllowPublicKeyRetrieval=True;SslMode=None;"
}
```

Run the project again:

```bash
dotnet restore
dotnet run
```

On first launch, the application creates missing EF Core tables and seeds the initial admin, categories, foods, and inventory. For production, use reviewed EF Core migrations instead of relying on `EnsureCreated`.

## 7. Configure eSewa

The project includes the eSewa sandbox endpoint and test product code in `appsettings.json`. The checkout flow creates a signed ePay v2 form and redirects the customer to eSewa.

For production, update these values with the credentials supplied by eSewa:

```json
"Payments": {
  "eSewa": {
    "Endpoint": "YOUR_ESEWA_ENDPOINT",
    "ProductCode": "YOUR_PRODUCT_CODE",
    "SecretKey": "YOUR_SECRET_KEY"
  }
}
```

The application must be reachable through a public HTTPS URL for gateway callbacks. Update `App:BaseUrl` to that public URL before testing callbacks.

## 8. Configure Khalti

Khalti is disabled until a secret key is supplied. You can configure it with environment variables.

### Windows PowerShell

```powershell
$env:Payments__Khalti__SecretKey = "YOUR_KHALTI_SECRET_KEY"
$env:Payments__Khalti__PublicKey = "YOUR_KHALTI_PUBLIC_KEY"
$env:App__BaseUrl = "https://your-public-domain.example"
dotnet run
```

### Linux/macOS

```bash
export Payments__Khalti__SecretKey="YOUR_KHALTI_SECRET_KEY"
export Payments__Khalti__PublicKey="YOUR_KHALTI_PUBLIC_KEY"
export App__BaseUrl="https://your-public-domain.example"
dotnet run
```

Do not commit real payment secrets to GitHub. Use environment variables or a production secret manager.

## 9. Stop the application

Return to the terminal running `dotnet run` and press:

```text
Ctrl+C
```

## 10. Common problems

| Problem | Fix |
| --- | --- |
| `dotnet is not recognized` | Install the .NET 8 SDK and reopen the terminal. |
| Port already in use | Stop the previous process or run `dotnet run --urls http://localhost:5050`. |
| MySQL connection failure | Check that MySQL is running and verify server, port, database, username, and password. |
| Empty menu after a restart | Confirm that MySQL is running on port 3307 and that the application is using the correct database and password. |
| Khalti does not open | Add the Khalti secret key and set a publicly reachable HTTPS `App:BaseUrl`. |
| HTTPS certificate warning | Use the HTTP URL shown by `dotnet run` or trust the development certificate. |

## 11. Test the eSewa sandbox payment flow

The eSewa payment request now creates a fresh transaction UUID for every payment attempt. The UUID format is similar to `ORDER-42-9f7e...`; it contains only letters, numbers, and hyphens, which satisfies eSewa’s transaction UUID format. The UUID is stored in the `Payment.TransactionId` column before the customer is redirected to eSewa.

### Prepare local callback URLs

For local browser testing, the application is configured with:

```json
"App": {
  "BaseUrl": "http://localhost:5062"
}
```

Keep the ASP.NET Core terminal running. The browser can return to localhost after the eSewa sandbox payment. For a deployed environment, change `App:BaseUrl` to the public HTTPS URL of the deployed application.

### Start a fresh test order

1. Start MySQL and confirm the application connection works.
2. Start the application with `dotnet run`.
3. Open `http://localhost:5062`.
4. Register a new customer or sign in to an existing customer account.
5. Add a food item to the cart.
6. Open the cart and continue to checkout.
7. Select **eSewa** and place the order.
8. The application saves a new payment row and redirects the browser to the configured eSewa sandbox form.
9. On eSewa, sign in with the sandbox eSewa ID and password provided by eSewa.
10. Complete the sandbox verification step. The eSewa UAT documentation states that the testing token is `123456`.
11. Confirm the payment in eSewa.
12. eSewa redirects the browser to `/Payment/Success` with its Base64-encoded response.
13. The application verifies the eSewa response signature, transaction UUID, product code, amount, and `COMPLETE` status before marking the order as paid.
14. Open **My orders** and confirm that the order status is `Confirmed` and payment status is `Paid`.

### Verify the UUID in MySQL Workbench

After selecting eSewa at checkout, inspect the payment record:

```sql
USE thali_spice;
SELECT PaymentId, OrderId, TransactionId, PaymentMethod, Amount, Status, PaidAt
FROM Payment
ORDER BY PaymentId DESC
LIMIT 5;
```

Each new eSewa attempt should have a different `TransactionId`. Do not reuse a previous transaction UUID, and do not repeatedly refresh an old eSewa form.

### Fixing `Duplicate transaction UUID`

That error means eSewa received a transaction UUID that it has already seen. The corrected application no longer uses only `ORDER-{OrderId}`. It creates a new UUID with `Guid.NewGuid()` for every new eSewa initiation and stores it before rendering the handoff form.

After pulling the latest code, stop and restart the application, create a new order or start a new eSewa attempt, and do not reuse an old browser tab or previously submitted payment form. If the same error remains, inspect the latest `Payment.TransactionId` value in Workbench and confirm that the new attempt contains the random suffix.

The application also rejects unsigned or mismatched callbacks. It will not mark an order as paid merely because someone visits the success URL; the returned eSewa response must have a valid signature and must match the stored UUID, product code, and total amount.

### Sandbox versus production

The repository is configured for the eSewa UAT endpoint and the UAT product code `EPAYTEST`. UAT credentials and balances are separate from real eSewa accounts. Before accepting real money, replace the endpoint, product code, secret key, and public HTTPS callback URL with the merchant credentials issued for production by eSewa.
