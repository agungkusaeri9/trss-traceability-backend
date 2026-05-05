# TraceabilitySystem — Clean Architecture Template

A robust and scalable backend boilerplate built with **ASP.NET Core 10**, following the principles of **Clean Architecture**, **Repository Pattern**, and **Service Pattern**.

## 🚀 Features

- **Clean Architecture**: Separation of concerns into Domain, Application, Infrastructure, and API layers.
- **Entity Framework Core**: Using Npgsql for PostgreSQL integration.
- **Generic Repository & Service Pattern**: Powerful base classes to reduce boilerplate code for CRUD operations.
- **Entity Framework Core**: Using Npgsql for PostgreSQL integration.
- **JWT Authentication**: Secure API access with Access & Refresh Tokens.
- **Swagger Documentation**: Pre-configured with JWT Bearer support (HTTP Scheme).
- **Global Exception Handling**: Custom middleware for consistent error responses.
- **FluentValidation**: Automatic request validation with clear error messages.
- **AutoMapper**: Clean DTO to Entity mapping.
- **Logging**: Configured with Serilog for Console and File logging.

## 🏗️ Project Structure

```text
src/
 ├── TraceabilitySystem.Domain          # Pure Business Models & Interfaces
 ├── TraceabilitySystem.Application     # Use Cases, Services, DTOs & Mappings
 ├── TraceabilitySystem.Infrastructure  # DB Context, Repositories & External Services
 ├── TraceabilitySystem.API             # Controllers, Middlewares & Entry Point
 └── TraceabilitySystem.Shared          # Common Constants, Helpers & Exceptions
```

## 🛠️ Getting Started

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/)

### 2. Configuration
Update the connection string in `src/TraceabilitySystem.API/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=TraceabilitySystemDb;Username=postgres;Password=your_password"
}
```

### 3. Database Migrations
Run the following commands to initialize the database:

```powershell
# Add first migration
dotnet ef migrations add InitialCreate --project src/TraceabilitySystem.Infrastructure --startup-project src/TraceabilitySystem.API

# Update database
dotnet ef database update --project src/TraceabilitySystem.Infrastructure --startup-project src/TraceabilitySystem.API
```

### 4. Running the Application
Use `dotnet watch` to run the API with hot-reload:

```powershell
dotnet watch run --project src/TraceabilitySystem.API
```

Access the Swagger UI at: `http://localhost:5039/swagger`

1. Register/Login via the `Auth` controller.
2. Copy the `accessToken`.
3. Click the **Authorize** button in Swagger.
4. **Paste the token only** (the "Bearer " prefix is added automatically).
5. Click **Authorize**.

## 📝 License
This project is licensed under the MIT License.
