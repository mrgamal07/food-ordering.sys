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
| Empty menu after a restart | You are using in-memory demo mode; configure MySQL for persistent data. |
| Khalti does not open | Add the Khalti secret key and set a publicly reachable HTTPS `App:BaseUrl`. |
| HTTPS certificate warning | Use the HTTP URL shown by `dotnet run` or trust the development certificate. |
