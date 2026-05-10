<div align="center">

# 💊 Pharmacy System API

**A production-ready RESTful API for pharmacy management**
built with ASP.NET Core 9 · Clean Architecture · JWT Auth

<br/>

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-9.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-Latest-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![JWT](https://img.shields.io/badge/JWT-Auth-000000?style=flat-square&logo=jsonwebtokens&logoColor=white)](https://jwt.io/)
[![Scalar](https://img.shields.io/badge/Scalar-Docs-6C47FF?style=flat-square)](https://scalar.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)

</div>

---

## 📌 Overview

**Pharmacy System API** helps pharmacies handle their day-to-day operations through a clean and secure REST API.
It covers everything from medicine inventory and expiry tracking to sales invoicing and business reports —
secured behind role-based JWT authentication.

> Built by **Abdallah Sayed** as a full-featured personal backend project using ASP.NET Core 9 and Clean Architecture.

---

## ✨ Features at a Glance

| Module | Highlights |
|---|---|
| 🔐 **Auth** | JWT login · Role-based (Admin / Pharmacist) · Secure password hashing |
| 💊 **Medicines** | Add / Update / Soft-delete · Search & filter · Expiry tracking |
| 📦 **Inventory** | Real-time stock tracking · Low-stock alerts · Auto-deduct on sale |
| 🧾 **Sales** | Multi-item invoices · Historical price capture · Transaction history |
| 🗂 **Categories** | Organize medicines · Admin CRUD · Filter support |
| 📊 **Reports** | Top sellers · Daily revenue · Inventory value · Low-stock & expiry alerts |

---

## 🛠️ Tech Stack

| Technology | Version | Purpose |
|---|---|---|
| ASP.NET Core Web API | 9.0 | Backend framework |
| Entity Framework Core | 9.0 | ORM & migrations |
| SQL Server | Latest | Database |
| JWT Bearer | Latest | Authentication & authorization |
| Scalar / OpenAPI | 2.x | Interactive API docs |
| AutoMapper | Latest | DTO object mapping |

---

## 🏗️ Architecture

This project follows **Clean Architecture** — each layer has a single responsibility and only depends on inner layers.

```
pharmacy-management-system/
│
├── PharmacySystem.API              ← Presentation Layer
│   ├── Controllers/
│   ├── Middlewares/
│   └── Program.cs
│
├── PharmacySystem.Application      ← Application Layer
│   ├── Services/
│   ├── DTOs/
│   └── Mapping Profiles/
│
├── PharmacySystem.Infrastructure   ← Infrastructure Layer
│   ├── Repositories/
│   ├── AppDbContext/
│   └── Migrations/
│
└── PharmacySystem.Core             ← Domain Layer (zero external dependencies)
    ├── Entities/
    ├── Interfaces/
    └── Enums/
```

### Dependency Flow

```
API ──► Application ──► Core
API ──► Infrastructure ──► Core
```

> `PharmacySystem.Core` has **no external dependencies** — it is the heart of the system.

---

## 🗃️ Database Design

<details>
<summary><b>Click to expand all tables</b></summary>

### `User`
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string | Full name |
| Email | string | Unique |
| PasswordHash | string | Never stored as plain text |
| Role | string | `Admin` or `Pharmacist` |
| CreatedAt | DateTime | Auto-set on creation |

### `Medicine`
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string | |
| Description | string? | Optional |
| Price | decimal(18,2) | Current selling price |
| Stock | int | Available units |
| MinStock | int | Low-stock alert threshold |
| ExpiryDate | DateTime | |
| IsActive | bool | Soft-delete flag (default: `true`) |
| CategoryId | int | FK → Category |
| CreatedAt | DateTime | |

### `Category`
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string | e.g. Antibiotics, Vitamins |

### `Sale`
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| SaleDate | DateTime | |
| TotalAmount | decimal(18,2) | Invoice total |
| UserId | int | FK → User (Pharmacist) |

### `SaleItem`
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Quantity | int | Units sold |
| UnitPrice | decimal(18,2) | Price at time of sale (historical) |
| SaleId | int | FK → Sale |
| MedicineId | int | FK → Medicine |

</details>

---

## 📡 API Endpoints

<details>
<summary><b>🔑 Authentication</b></summary>

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new user | Public |
| `POST` | `/api/auth/login` | Login and get JWT token | Public |
| `POST` | `/api/auth/change-password` | Change your password | Authenticated |

</details>

<details>
<summary><b>💊 Medicines</b></summary>

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `GET` | `/api/medicines` | All active medicines (paginated) | Authenticated |
| `GET` | `/api/medicines/{id}` | Get medicine by ID | Authenticated |
| `POST` | `/api/medicines` | Add new medicine | Admin |
| `PUT` | `/api/medicines/{id}` | Update medicine | Admin |
| `DELETE` | `/api/medicines/{id}` | Soft-delete medicine | Admin |
| `GET` | `/api/medicines/low-stock` | Below minimum stock | Authenticated |
| `GET` | `/api/medicines/expiring` | Near expiry date | Authenticated |

</details>

<details>
<summary><b>🗂️ Categories</b></summary>

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `GET` | `/api/categories` | All categories | Authenticated |
| `POST` | `/api/categories` | Create category | Admin |
| `PUT` | `/api/categories/{id}` | Update category | Admin |
| `DELETE` | `/api/categories/{id}` | Delete category | Admin |

</details>

<details>
<summary><b>🧾 Sales</b></summary>

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `POST` | `/api/sales` | Record new sale invoice | Pharmacist |
| `GET` | `/api/sales` | Sales history (paginated) | Authenticated |
| `GET` | `/api/sales/{id}` | Sale details with items | Authenticated |

</details>

<details>
<summary><b>📊 Reports</b></summary>

| Method | Endpoint | Description | Access |
|---|---|---|---|
| `GET` | `/api/reports/top-medicines` | Top selling medicines | Admin |
| `GET` | `/api/reports/daily-revenue` | Daily revenue summary | Admin |
| `GET` | `/api/reports/stock-value` | Total inventory value | Admin |

</details>

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### 1. Clone the repository

```bash
git clone https://github.com/AbdallahSay/pharmacy-management-system.git
cd pharmacy-management-system
```

### 2. Configure `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=PharmacyDB;Trusted_Connection=True;"
  },
  "JWT": {
    "Key": "your-super-secret-key-here",
    "Issuer": "PharmacySystem",
    "Audience": "PharmacySystemUsers",
    "ExpireDays": 7
  }
}
```

### 3. Apply database migrations

```bash
dotnet ef database update \
  --project PharmacySystem.Infrastructure \
  --startup-project PharmacySystem.API
```

### 4. Run the project

```bash
dotnet run --project PharmacySystem.API
```

### 5. Explore the API docs

```
https://localhost:7266/scalar/pharmacy
```

---

## 🔐 Roles & Permissions

| Permission | Admin | Pharmacist |
|---|:---:|:---:|
| Manage medicines | ✅ | ➖ Read only |
| Manage categories | ✅ | ❌ |
| Record sales | ✅ | ✅ |
| View inventory alerts | ✅ | ✅ |
| Access reports | ✅ | ❌ |
| Manage users | ✅ | ❌ |

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 👤 Author

**Abdallah Sayed**

[![GitHub](https://img.shields.io/badge/GitHub-AbdallahSay-181717?style=flat-square&logo=github)](https://github.com/AbdallahSay)

---

<div align="center">
  <sub>Built with ❤️ using ASP.NET Core 9 · Clean Architecture · SQL Server</sub>
</div>
