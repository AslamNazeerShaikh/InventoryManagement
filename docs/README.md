# Inventory Management System - Documentation

## 📋 Overview

Welcome to the comprehensive documentation for the **Inventory Management System** - a robust solution for managing medical equipment inventory with expiry tracking, assignment management, and real-time visibility designed for healthcare environments with Nurse Practitioners.

The latest implementation includes a security/architecture hardening refactor and a Result design pattern for application outcomes: unified JWT configuration, Microsoft PBKDF2 password hashing, hashed refresh tokens, authenticated idempotency, optimistic concurrency, centralized exception handling, automatic validation, configurable CORS/rate limiting, structured Serilog logging, and `/health` checks. HTTP response bodies still use the `ApiResponse<T>` envelope (`isSuccess`, `message`, `data`, `errors`) so client JSON shape remains stable.

## 🗂️ Documentation Structure

### 1. [API Testing Guide](API-Testing-Guide.md) 📡

**Complete step-by-step testing documentation for all API endpoints**

- **Pre-seeded Admin Access**: Local development credentials and secure production seeding notes
- **Controller-by-Controller Testing**: Auth, Users, Inventory, Assignments, Dashboard, and Health
- **Security Testing**: JWT, refresh-token, rate-limit, validation, idempotency, and authorization scenarios
- **Testing Tools**: PowerShell, cURL, and Postman examples

**🎯 Use this when**: You need to test the API endpoints systematically.

---

### 2. [UI Testing Guide](UI-Testing-Guide.md) 🖥️

**End-to-end front-end testing for the MediStock web client**

- **Running the app**: start the API + Next.js client with the same-origin proxy
- **Role-based journeys**: Admin, Provider (Nurse Practitioner) and Staff permutations
- **Feature-by-feature steps**: Dashboard, Inventory, Assignments, Users, and Profile
- **Permission & action matrix**: what each role can do on every page, plus validation/error cases

**🎯 Use this when**: You need to test or understand the web UI and its permissions.

---

### 3. [Database Schema](Database-Schema.md) 🗄️

**Comprehensive database design and relationship documentation**

- **Table Structures**: Users, Inventories, InventoryAssignments, and IdempotentRequests
- **Concurrency Columns**: `ConcurrencyToken` fields on domain entities
- **Indexes & Performance**: Optimized indexes for common queries and idempotency cleanup
- **Migration Management**: EF Core migration commands and versioning

**🎯 Use this when**: You need to understand the database structure or relationships.

---

### 4. [Domain Model](Domain-Model.md) 🏗️

**Domain-driven design documentation covering entities, DTOs, options, security abstractions, and business logic**

- **Core Entities**: User, Inventory, InventoryAssignment, IdempotentRequest, and BaseEntity
- **DTOs & Validation**: Request/response models with DataAnnotations and automatic API validation
- **Result Pattern**: `Result<T>` service outcomes mapped to HTTP status codes by the API layer
- **Security Abstractions**: `JwtOptions`, `ITokenService`, `IPasswordHasher`, `ISecretClient`
- **Repository Interfaces**: Read-optimized data access contracts with cancellation support

**🎯 Use this when**: You need to understand or extend the business domain.

---

### 5. [Data Flow & Architecture](Data-Flow-Architecture.md) 🔄

**System architecture and data flow patterns throughout the application layers**

- **Clean Architecture Layers**: Presentation, Application, Domain, and Infrastructure separation
- **Security Pipeline**: JWT key resolution, token validation, rate limiting, CORS, and health checks
- **Idempotency Flow**: Authenticated locking, request hashing, replay, bounded caching, and cleanup
- **Result + Response Mapping**: Application services return `Result<T>`; `ApiControllerBase` maps outcomes to HTTP status codes while preserving `ApiResponse<T>` bodies
- **Error Handling**: Centralized `IExceptionHandler` and uniform `ApiResponse` responses

**🎯 Use this when**: You need to understand how data moves through the system.

---

## 🚀 Quick Start Guide

### Prerequisites Setup

1. **Install .NET SDK** required by the solution.
2. **Trust HTTPS certificates** for development:
   ```powershell
   dotnet dev-certs https --trust
   ```

### Project Setup & Running

```powershell
cd "c:\Users\aslams\source\repos\localDev\InventoryManagement"
dotnet restore
cd server\InventoryManagement.API
dotnet build
dotnet run --launch-profile https
```

Alternative:

```powershell
cd "c:\Users\aslams\source\repos\localDev\InventoryManagement\server\InventoryManagement.API"
dotnet run --urls="https://localhost:7178;http://localhost:5067"
```

### After Startup

1. **API Available at**: `https://localhost:7178`
2. **OpenAPI JSON**: `https://localhost:7178/openapi/v1.json`
3. **Scalar API Reference**: `https://localhost:7178/scalar/v1` (Development only)
4. **Health Check**: `https://localhost:7178/health`
5. **Local Development Admin Login**:
   - **Email**: `admin@inventorymanagement.com`
   - **Password**: `ChangeMe_LocalDev!2026`

> Production should set `SeedData:AdminPassword`. If it is empty, the app generates a strong random admin password and logs it once at startup.

### Configuration Highlights

Current configuration sections are:

- `ConnectionStrings:DefaultConnection` - SQLite database path.
- `Serilog` - console and rolling file logging (`logs\InventoryManagement-.txt`).
- `Jwt` - single source of truth for token generation and validation: `KeySource`, `Key`, `KeySecretName`, `Issuer`, `Audience`, `AccessTokenMinutes`, `RefreshTokenDays`, `ClockSkewSeconds`.
- `Idempotency` - key length, response cache cap, lock duration, retention, cleanup interval/batch size, and excluded path prefixes.
- `Cors` - explicit `AllowedOrigins` and `AllowCredentials`; applied in every environment.
- `RateLimiting:Auth` - fixed-window limits for `/api/auth/login` and `/api/auth/refresh` (default 10 requests per 60 seconds per remote IP).
- `SeedData` - admin email/password seeding.

JWT signing keys can be supplied locally with inline configuration, user-secrets, environment overrides, or mounted files. For cloud secret stores, use `Jwt:KeySource=CloudSecret`, set `Jwt:KeySecretName`, and register a cloud-backed `ISecretClient`. Keys under 256 bits are rejected at startup. The old `JwtSettings`, `CORS`, and `ApiSettings` sections are no longer used.

### Result Pattern & API Responses

Application services return `Result<T>` for expected business outcomes instead of throwing. Controllers inherit from `ApiControllerBase`, which maps each `ResultErrorType` to an HTTP status code while keeping the response body as `ApiResponse<T>`.

| Result outcome | HTTP status | ApiResponse body |
| -------------- | ----------- | ---------------- |
| `Success` | 200 OK, or 201 Created for create endpoints | `isSuccess: true`, `message`, `data`, empty `errors` |
| `NotFound` | 404 Not Found | `isSuccess: false`, `message`, `errors` |
| `Conflict` | 409 Conflict | `isSuccess: false`, `message`, `errors` |
| `Validation` | 400 Bad Request | `isSuccess: false`, `message`, `errors` |
| `Unauthorized` | 401 Unauthorized | `isSuccess: false`, `message`, `errors` |
| `Forbidden` | 403 Forbidden | `isSuccess: false`, `message`, `errors` |
| `Failure` | 400 Bad Request | `isSuccess: false`, `message`, `errors` |

Examples: missing entities return 404, duplicate barcode/email conflicts return 409, and invalid login returns 401 without changing the JSON envelope.


### Troubleshooting Startup

#### JWT Key Issues

- `Jwt:Key` is required when `Jwt:KeySource` is `Inline`.
- `Jwt:KeySecretName` is required when the key is resolved from environment, file, or cloud secrets.
- The resolved key must be at least 32 UTF-8 bytes.

#### Database Issues

- Database auto-migrates and seeds on first run.
- Development location: `server\InventoryManagement.Infrastructure\inventory.db`.
- To reset: stop the app, delete the database file, restart the app.

---

## 🏥 Business Context

### System Purpose

This inventory management system addresses healthcare offices that:

- **Receive medical equipment** for distribution to Nurse Practitioners
- **Track expiry dates** with configurable advance alerts
- **Manage assignments** with return tracking
- **Maintain real-time visibility** of inventory status and availability
- **Support barcode scanning** for efficient inventory operations

### Key Features Documented

#### 🔐 Security & Authentication

- **JWT authentication** with a single strongly-typed `JwtOptions` configuration source
- **Microsoft password hashing** using PBKDF2-HMAC-SHA256 via `PasswordHasher`
- **Refresh-token hashing**: only SHA-256 hashes are stored server-side
- **Role/policy-based authorization** (Admin, NursePractitioner, Staff)
- **Auth endpoint rate limiting** returning HTTP 429
- **CORS in all environments** with explicit allowed origins

#### 📦 Inventory Management

- **Complete CRUD operations** with automatic validation
- **Barcode and serial-number support**
- **Expiry and low stock monitoring**
- **Optimistic concurrency** to prevent lost stock updates and overselling

#### 👥 Assignment Workflow

- **Equipment assignment and return processing**
- **Transactional create/return operations** via `IUnitOfWork.ExecuteInTransactionAsync`
- **Overdue assignment monitoring**
- **Quantity tracking** using total and available inventory quantities

#### 📊 Dashboard & Reporting

- **Real-time statistics** for inventory and assignments
- **Multiple alert types** (expiry, low stock, overdue)
- **Sequential aggregate loading** to avoid shared DbContext concurrency errors

---

## 🛠️ Technical Architecture

### Clean Architecture Implementation

```
API Layer (Controllers, pipeline, validation, auth)
    ↓
Application Layer (Services and mapping)
    ↓
Domain Layer (Entities, DTOs, options, interfaces, exceptions)
    ↓
Infrastructure Layer (EF Core, repositories, tokens, hashing, secrets, idempotency)
```

The Application layer no longer depends on Infrastructure; options and security abstractions live in Domain, with implementations registered by Infrastructure.

### Technology Stack

- **Framework**: ASP.NET Core with controllers, automatic model validation, and `ApiControllerBase` Result mapping
- **Database**: SQLite with Entity Framework Core
- **Authentication**: JWT bearer tokens with HMAC-SHA256 signing keys resolved by `IJwtSigningKeyProvider`
- **Password Hashing**: Microsoft `PasswordHasher` (PBKDF2-HMAC-SHA256)
- **Logging**: Serilog bootstrap + full config, console + rolling file sinks
- **API Documentation**: OpenAPI with Scalar in Development

---

## 📚 API Endpoint Summary

### Authentication (`/api/auth`)

- `POST /login` - User authentication (rate limited)
- `POST /refresh` - Token refresh (rate limited)
- `POST /logout` - Revoke refresh token
- `POST /change-password` - Password change and refresh-token revocation
- `GET /me` - Current user info

### Users (`/api/users`)

- `GET /`, `GET /paged`, `POST /`, `GET /{id}`, `GET /by-email/{email}`, `PUT /{id}`, `DELETE /{id}`, `GET /nurse-practitioners`, `GET /active`

### Inventory (`/api/inventory`)

- `GET /`, `GET /paged`, `POST /`, `GET /{id}`, `GET /barcode/{barcode}`, `POST /search`, `GET /available`, `GET /expiring`, `GET /low-stock`, `GET /category/{category}`, `PUT /{id}`, `DELETE /{id}`

### Assignments (`/api/inventoryassignments`)

- `GET /`, `GET /paged`, `POST /`, `GET /{id}`, `GET /user/{userId}`, `GET /my-assignments`, `GET /active`, `GET /active/user/{userId}`, `PUT /{id}`, `POST /return`, `GET /overdue`, `GET /history/inventory/{id}`

### Dashboard (`/api/dashboard`)

- `GET /stats`, `GET /overview`, `GET /alerts/summary`, `GET /alerts/expiry`, `GET /alerts/low-stock`, `GET /alerts/overdue`, `GET /recent-inventories`, `GET /recent-assignments`

### Health

- `GET /health` - Application health endpoint

---

## 📞 Support Information

### Default Admin Access

- **Email**: `admin@inventorymanagement.com`
- **Local Development Password**: `ChangeMe_LocalDev!2026`
- **Capabilities**: Full system access for initial setup

### Database Reset (Development)

1. Stop the application.
2. Delete `server\InventoryManagement.Infrastructure\inventory.db`.
3. Restart the application.

### Logging Location

- **Log Files**: `server\InventoryManagement.API\logs\InventoryManagement-.txt`
- **Rotation**: Hourly with 168 retained files by default
- **Format**: Structured Serilog output with source context

---

## 📋 Documentation Checklist

- ✅ **API Testing Guide**: Complete endpoint testing with security/error scenarios
- ✅ **UI Testing Guide**: Front-end feature walkthroughs, role permutations, and permission matrix
- ✅ **Database Schema**: Full table structures with concurrency and idempotency details
- ✅ **Domain Model**: Business entities, DTOs, options, and interfaces
- ✅ **Data Flow & Architecture**: System patterns and implementation guidance
- ✅ **Overview Documentation**: Navigation and quick start guide

All documentation is current as of the security/architecture hardening and Result-pattern refactor.

---

_This documentation is maintained alongside the codebase and should be updated when business rules, API endpoints, or data structures change._
