# Thali & Spice — Single-Restaurant Ordering System

A single-restaurant online food ordering and management system built with **ASP.NET Core MVC (.NET 8)**, **HTML/CSS/Bootstrap 5/JavaScript**, **MySQL**, and **Entity Framework Core**. The application keeps `Customer` and `Admin` in separate tables and implements exactly the nine requested tables: `Customer`, `Admin`, `Food_Category`, `Food`, `Orders`, `Order_Details`, `Payment`, `Inventory`, and `Sold_Items`.

## Included capabilities

Customers can register, sign in, browse searchable and category-filtered menu items, add dishes to a session cart, check out with a delivery address, choose cash on delivery, eSewa, or Khalti, and track fulfillment status. The admin area is protected by an `Admin` role and provides dashboard KPIs, menu and category management, inventory updates, order-status operations, and sold-item revenue reporting.

The eSewa flow builds a signed ePay v2 form using the configured product code and secret, then handles success and failure callbacks. The Khalti flow initiates a payment with the configured secret key and validates the callback with Khalti's lookup endpoint. Keep gateway secrets outside source control in production by using environment variables or a secret manager.

## Run locally

The project falls back to an EF Core in-memory database when `ConnectionStrings:DefaultConnection` is blank, so the demo can be started immediately:

```bash
export PATH="$HOME/.dotnet:$PATH"
cd SingleRestaurantOrdering
dotnet run
```

The seeded development accounts are:

| Account | Email | Password |
| --- | --- | --- |
| Admin | `admin@thaliandspice.com` | `Admin@123` |

A customer account can be created at `/Auth/Register`.

## MySQL configuration

Set the connection string before starting the application. This project is configured for the user’s local MySQL Workbench server on port `3307`. Replace `YOUR_MYSQL_PASSWORD` with the password for the MySQL user configured in Workbench. EF Core's `EnsureCreated` creates the database tables on first launch for a fresh database; for production, replace it with reviewed migrations.

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3307;Database=thali_spice;User=root;Password=YOUR_MYSQL_PASSWORD;AllowPublicKeyRetrieval=True;SslMode=None;"
}
```

The table relationships are configured in `Data/ApplicationDbContext.cs`. `Food_Category -> Food`, `Customer -> Orders`, `Orders -> Order_Details`, `Food -> Order_Details`, `Orders -> Payment`, `Food -> Inventory`, `Orders -> Sold_Items`, and `Food -> Sold_Items` are explicit foreign-key relationships with deliberate delete behavior.

## Payment configuration

The eSewa defaults are the sandbox values and endpoint. Replace them for production. Khalti remains disabled until `Payments:Khalti:SecretKey` and `Payments:Khalti:PublicKey` are supplied. The callback URL must be publicly reachable over HTTPS in a deployed environment, and `App:BaseUrl` must match that origin.

```bash
export Payments__Khalti__SecretKey="your-khalti-secret"
export Payments__Khalti__PublicKey="your-khalti-public-key"
export App__BaseUrl="https://your-domain.example"
```

## Main routes

| Area | Route | Purpose |
| --- | --- | --- |
| Customer | `/` | Menu, search, categories, and add-to-cart |
| Customer | `/Cart` | Cart and checkout |
| Customer | `/Orders` | Order history and tracking |
| Auth | `/Auth/Login` | Customer or admin login |
| Auth | `/Auth/Register` | Customer registration |
| Admin | `/Admin` | Operations dashboard |
| Admin | `/Admin/Foods` | Menu and availability management |
| Admin | `/Admin/Inventory` | Stock and reorder levels |
| Admin | `/Admin/SoldItems` | Sold-item report |

## Production notes

Use HTTPS, rotate the seeded admin password, configure a persistent data-protection key ring for multi-instance cookie authentication, add EF Core migrations, and replace the demo image with licensed restaurant photography. Payment callbacks should be tested with the provider's current sandbox documentation and credentials before enabling live payments.
