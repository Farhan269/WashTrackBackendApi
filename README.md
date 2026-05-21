# WashTrack Backend

A robust, secure, and scalable backend API for managing **wash receiving, processing, and delivery operations**. This system is designed to handle work orders, wash transactions, process stages, and role-based user access with enterprise-grade architecture and best practices.

---

## 🚀 Overview

The **wsahRecieveDelivary API** serves as the core backend for wash operation workflows. It enables organizations to efficiently track work orders, monitor item movement through multiple washing stages, manage user permissions, and export operational data for reporting and analysis.

The API is built with **.NET 8** and follows a clean **N-Tier architecture**, ensuring maintainability, scalability, and long-term growth.

---

## ✨ Key Features

### 🔐 User & Role Management

* User registration and authentication
* Role-based authorization (Admin, Operator, etc.)
* Secure password hashing using BCrypt

### 🛡 Authentication & Authorization

* JWT-based stateless authentication
* Fine-grained permission control per process stage
* Secure access to protected endpoints

### 🧾 Work Order Management

* Create, view, update, and delete work orders
* Track ownership and update history
* Associate work orders with wash transactions and process stages

### 🧼 Wash Transaction Tracking

* Record item movement across washing stages
* Maintain a complete lifecycle history
* Ensure accurate stage-wise accountability

### 🔄 Process Stage Management

* Define customizable wash stages (e.g., Received, Washing, Delivered)
* Assign user permissions per stage
* Maintain real-time stage balances

### 📤 Data Export

* Export reports to **CSV** and **Excel** formats
* Ideal for auditing, reporting, and analytics

---

## 🧰 Technology Stack

| Category         | Technology              |
| ---------------- | ----------------------- |
| Framework        | .NET 8                  |
| API              | ASP.NET Core Web API    |
| ORM              | Entity Framework Core 8 |
| Database         | SQL Server              |
| Authentication   | JWT (JSON Web Tokens)   |
| API Docs         | Swagger (Swashbuckle)   |
| Security         | BCrypt.Net              |
| Export Utilities | CsvHelper, EPPlus       |

---

## 🏗 Architecture

The project follows a **clean N-Tier architecture**, separating responsibilities across well-defined layers:

### 1️⃣ Presentation Layer (Controllers)

* Handles HTTP requests and responses
* Exposes RESTful endpoints
* Delegates all business logic to services

### 2️⃣ Business Logic Layer (Services)

* Contains core application logic
* Enforces business rules and validations
* Acts as a mediator between controllers and data access

### 3️⃣ Data Access Layer (Data)

* Manages database interactions using EF Core
* Includes `ApplicationDbContext` as the database session
* Ensures efficient querying and persistence

### 4️⃣ Domain Model (Entities)

* Represents core business objects
* Maps directly to database tables

### 5️⃣ Data Transfer Objects (DTOs)

* Defines API request and response contracts
* Prevents over-posting and tight coupling
* Improves API security and clarity

---

## 🗄 Database Design

The database schema is optimized for tracking wash operations and maintaining data integrity.

### 🔗 Entity Relationships

* **User ↔ Role**

  * Many-to-many via `UserRole`

* **User ↔ ProcessStage**

  * Many-to-many via `UserProcessStageAccess`
  * Includes permissions (`CanView`, `CanEdit`)

* **WorkOrder**

  * Created and updated by users
  * Linked to multiple wash transactions
  * Maintains stage-wise balances

* **WashTransaction**

  * Belongs to one work order and one process stage
  * Tracks item movement history

* **ProcessStage**

  * Represents each step in the wash lifecycle
  * Linked to transactions and balances

* **ProcessStageBalance**

  * Summary table showing item balance per stage
  * Optimized for fast operational queries

This structure ensures **traceability**, **performance**, and **data consistency**.

---

## ✅ Prerequisites

Ensure the following tools are installed before setup:

* .NET 8 SDK
* SQL Server / SQL Server Express
* Visual Studio or VS Code
* Git

---

## ⚙️ Getting Started

### 1️⃣ Clone the Repository

```bash
git clone <repository-url>
cd wsahRecieveDelivary
```

### 2️⃣ Configure Database Connection

Update `appsettings.json` with your SQL Server details:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=wsahRD;User Id=USER;Password=PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

### 3️⃣ Apply Migrations

```bash
dotnet ef database update
```

### 4️⃣ Run the Application

```bash
dotnet run
```

The API will start at:

```
http://localhost:5000
```

(or the port defined in `launchSettings.json`)

---

## 📚 API Documentation

The API is fully documented using **Swagger**.

Access Swagger UI at:

```
http://localhost:<port>/swagger
```

### Main Controllers

* **AuthController** – Authentication & login
* **UserController** – User and role management
* **WorkOrderController** – Work order operations
* **WashTransactionController** – Transaction lifecycle management
* **ProcessStageController** – Wash stage configuration

---

## 🔑 Authentication Usage

Protected endpoints require a JWT token in the request header:

```
Authorization: Bearer <your-jwt-token>
```

Tokens are issued upon successful login via the authentication endpoint.

---

## 🔧 Configuration Notes

* All core settings are managed via `appsettings.json`
* JWT secrets and connection strings should be secured using:

  * User Secrets (development)
  * Environment variables (production)

---

## 📄 License

This project is licensed under the **MIT License**.

See the `LICENSE.md` file for more details.

---

## 🤝 Contribution & Support

Contributions, suggestions, and improvements are welcome.

For issues or feature requests, please open a ticket in the repository.

---

**Built with scalability, security, and maintainability in mind.**
