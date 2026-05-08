# TRSS Traceability System Backend

Backend API untuk sistem **Traceability Produksi** PT. Tokyo Radiator Selamat Sempurna (TRSS), dibangun dengan **ASP.NET Core 10** mengikuti prinsip **Clean Architecture**, **Repository Pattern**, dan **Service Pattern**.

## 🚀 Fitur Utama

- **Clean Architecture**: Pemisahan concerns ke dalam layer Domain, Application, Infrastructure, dan API.
- **Entity Framework Core + MySQL**: ORM dengan Pomelo MySQL Provider untuk manajemen database.
- **Generic Repository & Service Pattern**: Base class yang powerful untuk mengurangi boilerplate CRUD.
- **Dashboard Analytics API**: Endpoint `/api/dashboard` dengan sub-route `summary`, `stats`, dan `recent-logs` untuk monitoring produksi real-time.
- **Process Log Traceability**: Pencatatan jejak produksi per Issue Number dengan dukungan multi-proses dan multi-parameter.
- **Stored Procedure Integration**: `sp_insert_trace_data` untuk input data berkecepatan tinggi langsung dari HMI/IoT.
- **JWT Authentication**: Keamanan akses API dengan Access & Refresh Token.
- **Swagger Documentation**: Pre-configured dengan dukungan JWT Bearer.
- **Global Exception Handling**: Middleware custom untuk respons error yang konsisten.
- **FluentValidation**: Validasi request otomatis dengan pesan error yang jelas.
- **AutoMapper**: Pemetaan DTO ke Entity yang bersih.
- **Serilog**: Logging ke Console dan File.

## 🏗️ Struktur Proyek

```text
src/
 ├── TraceabilitySystem.Domain          # Pure Business Models & Interfaces
 ├── TraceabilitySystem.Application     # Use Cases, Services, DTOs & Mappings
 ├── TraceabilitySystem.Infrastructure  # DB Context, Repositories & External Services
 ├── TraceabilitySystem.API             # Controllers, Middlewares & Entry Point
 └── TraceabilitySystem.Shared          # Common Constants, Helpers & Exceptions
documentation/
 └── query-sql.md                       # Dokumentasi Stored Procedure & Contoh Query
```

## 📡 Endpoint Utama

### Dashboard
| Method | Endpoint | Deskripsi |
| :--- | :--- | :--- |
| `GET` | `/api/dashboard/summary` | Ringkasan jumlah produksi (Hari ini / Bulan ini / Total) |
| `GET` | `/api/dashboard/stats` | Data chart (Pie OK/NG, Bar Top Parts, Trend 7 Hari) |
| `GET` | `/api/dashboard/recent-logs` | Daftar log produksi terbaru |

### Process Log
| Method | Endpoint | Deskripsi |
| :--- | :--- | :--- |
| `GET` | `/api/processlogs` | Daftar semua log produksi (paginasi) |
| `GET` | `/api/processlogs/{id}` | Detail log berdasarkan ID |

### Config (Dev Only)
| Method | Endpoint | Deskripsi |
| :--- | :--- | :--- |
| `POST` | `/api/config/seed-all` | Seed semua data dummy |
| `POST` | `/api/config/clear-all-data` | Hapus semua data (Hati-hati!) |

> ⚠️ **Endpoint `/api/config` bersifat destruktif. Nonaktifkan di Production!**

## 🗄️ Stored Procedure (HMI Integration)

Untuk input data langsung dari mesin/HMI, gunakan stored procedure `sp_insert_trace_data`:

```sql
CALL sp_insert_trace_data(
    'ISS-00123',  -- Issue Number
    1,            -- p_is_ok: 1 = OK, 0 = NG (otomatis tambah suffix -R)
    'PRC-005',    -- Process Code
    '[{"parameter_code": "PRM-002", "val_num": 42.5, "val_txt": null, "val_bool": null}]'
);
```

Lihat dokumentasi lengkap di [`documentation/query-sql.md`](documentation/query-sql.md).

## 🛠️ Getting Started

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [MySQL 8.0+](https://dev.mysql.com/downloads/)

### 2. Konfigurasi
Update connection string di `src/TraceabilitySystem.API/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=trss_traceability_system;User=root;Password=your_password;"
}
```

### 3. Migrasi Database
```powershell
# Update database dengan semua migrasi yang ada
dotnet ef database update --project src/TraceabilitySystem.Infrastructure --startup-project src/TraceabilitySystem.API
```

### 4. Jalankan Aplikasi
```powershell
dotnet watch run --project src/TraceabilitySystem.API
```

Akses Swagger UI di: `http://localhost:5039/swagger`

### 5. Seed Data Awal
Setelah aplikasi berjalan, panggil endpoint berikut via Swagger atau Postman:
```
POST /api/config/seed-all
```

## 📝 Lisensi
This project is licensed under the MIT License.
