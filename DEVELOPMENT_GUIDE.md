# 🛠️ Development Guide — Step by Step

Panduan ini berisi langkah-langkah untuk menambahkan fitur baru mengikuti pola **Clean Architecture** dan **Generic Pattern** di boilerplate ini.

---

## 🏗️ 1. Domain Layer (The Core)
Semua definisi model bisnis tanpa dependensi luar.

1.  **Create Entity**: Tambahkan class di `src/TraceabilitySystem.Domain/Entities/`.
2.  **Create Repository Interface**: Tambahkan interface di `src/TraceabilitySystem.Domain/Interfaces/`.
    *   **Penting:** Warisi dari `IRepository<TEntity>`.
    *   *Contoh:* `public interface IProductRepository : IRepository<Product> { ... }`

---

## 🗄️ 2. Infrastructure Layer (Data & Tools)
Implementasi teknis dan akses data.

1.  **DbContext Configuration**: 
    *   Tambahkan `DbSet<Entity>` di `AppDbContext.cs`.
    *   Buat konfigurasi di `src/TraceabilitySystem.Infrastructure/Persistence/Configurations/`.
2.  **Implement Repository**:
    *   Buat class di `src/TraceabilitySystem.Infrastructure/Persistence/Repositories/`.
    *   **Penting:** Warisi dari `BaseRepository<TEntity>` dan implementasikan interface repository-nya.
    *   *Contoh:* `public class ProductRepository : BaseRepository<Product>, IProductRepository { ... }`

---

## 🧠 3. Application Layer (Business Logic)
Logika aplikasi dan mapping data menggunakan Generic Service.

1.  **Create DTOs**: Tambahkan models di `src/TraceabilitySystem.Application/DTOs/`.
2.  **Create Service Interface**:
    *   Buat interface di `src/TraceabilitySystem.Application/Interfaces/`.
    *   **Penting:** Warisi dari `IBaseService<TEntity, TDto>`.
3.  **Create Mapping Profile**: Daftarkan mapping di `src/TraceabilitySystem.Application/Mappings/`.
4.  **Implement Service**:
    *   Buat class di `src/TraceabilitySystem.Application/Services/`.
    *   **Penting:** Warisi dari `BaseService<TEntity, TDto>`.
    *   Ini akan otomatis memberikan fitur `GetByIdAsync`, `GetPagedAsync`, dan `DeleteAsync` tanpa kamu harus menulis kodenya lagi.
5.  **DI Registration**:
    *   Daftarkan Repository & Service baru di file `DependencyInjection.cs` masing-masing layer.

---

## 🌐 4. API Layer (Entry Point)
1.  **Create Controller**: Buat controller di `src/TraceabilitySystem.API/Controllers/`.
2.  **Use Base Methods**: Panggil method dari service (seperti `GetUsersAsync`) dan bungkus hasilnya dalam `ApiResponse` atau `PagedApiResponse`.

---

## 🚀 5. Database Migration
```powershell
# Tambah migration
dotnet ef migrations add NamaMigration --project src/TraceabilitySystem.Infrastructure --startup-project src/TraceabilitySystem.API

# Update database
dotnet ef database update --project src/TraceabilitySystem.Infrastructure --startup-project src/TraceabilitySystem.API
```

---

## 💡 Keuntungan Pola Generic
Dengan pola ini, jika kamu hanya butuh CRUD standar, kamu hampir tidak perlu menulis kode logika lagi di layer Repository maupun Service. Cukup buat class-nya dan "warisi" fungsinya dari base class yang sudah disediakan.
