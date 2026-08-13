# Database Schema Documentation - Inventory Management System

## Overview

This document provides a comprehensive overview of the database schema, including table structures, relationships, constraints, idempotency storage, optimistic concurrency, secure authentication storage, and data flow patterns. API-layer `Result<T>` outcomes map database conflicts/not-found cases to accurate HTTP status codes while preserving the `ApiResponse<T>` body.

## Database Technology

- **Database Engine**: SQLite
- **Development File Location**: `src\InventoryManagement.Infrastructure\inventory.db`
- **ORM**: Entity Framework Core
- **Migration Support**: Yes (Code-First approach)
- **Concurrency Strategy**: Provider-agnostic GUID `ConcurrencyToken` configured as an EF concurrency token

---

## Table Structures

### 1. Users Table

**Purpose**: Stores user accounts with role-based access control and hashed authentication material.

| Column                 | Type          | Constraints           | Description                                                |
| ---------------------- | ------------- | --------------------- | ---------------------------------------------------------- |
| Id                     | INTEGER       | PRIMARY KEY, IDENTITY | Auto-incrementing user identifier                          |
| Name                   | NVARCHAR(100) | NOT NULL              | Full/display name of the user                              |
| Email                  | NVARCHAR(256) | NOT NULL, UNIQUE      | User's email address (login identifier)                    |
| PasswordHash           | NVARCHAR      | NOT NULL              | Microsoft `PasswordHasher` PBKDF2-HMAC-SHA256 hash         |
| IsAdmin                | BIT           | NOT NULL              | Admin privilege flag                                       |
| IsProvider             | BIT           | NOT NULL              | Provider/Nurse Practitioner privilege flag                 |
| Role                   | NVARCHAR(50)  | NOT NULL              | Role: Admin, NursePractitioner, Staff                      |
| IsActive               | BIT           | NOT NULL              | Account enabled/disabled flag                              |
| LastLoginAt            | DATETIME      | NULL                  | Last successful login timestamp                            |
| RefreshToken           | NVARCHAR      | NULL                  | SHA-256 hash of refresh token; never the raw token          |
| RefreshTokenExpiryTime | DATETIME      | NULL                  | Refresh token expiration                                   |
| CreatedAt              | DATETIME      | NOT NULL              | Record creation timestamp                                  |
| UpdatedAt              | DATETIME      | NULL                  | Last update timestamp                                      |
| CreatedBy              | NVARCHAR      | NULL                  | User/system that created the record                        |
| UpdatedBy              | NVARCHAR      | NULL                  | User/system that last updated the record                   |
| IsDeleted              | BIT           | NOT NULL              | Soft-delete flag                                           |
| DeletedAt              | DATETIME      | NULL                  | Soft-delete timestamp                                      |
| DeletedBy              | NVARCHAR      | NULL                  | User/system that soft-deleted the record                   |
| ConcurrencyToken       | TEXT          | NOT NULL              | EF concurrency token rotated on every insert/update        |

**Indexes:**

- `IX_Users_Email` (UNIQUE)
- Role/activity indexes as configured for common lookups

**Sample Data:**

```sql
INSERT INTO Users (Email, PasswordHash, Name, Role, IsAdmin, IsProvider, IsActive, CreatedAt, ConcurrencyToken)
VALUES ('admin@inventorymanagement.com', '<PBKDF2 hash>', 'System Administrator', 'Admin', 1, 0, 1, CURRENT_TIMESTAMP, '<guid>');
```

Seeded admin email defaults to `admin@inventorymanagement.com`. Local Development password is `ChangeMe_LocalDev!2026`; production should configure `SeedData:AdminPassword` or use the generated password logged once at startup.

---

### 2. Inventories Table

**Purpose**: Stores medical equipment and supplies inventory.

| Column             | Type           | Constraints           | Description                                               |
| ------------------ | -------------- | --------------------- | --------------------------------------------------------- |
| Id                 | INTEGER        | PRIMARY KEY, IDENTITY | Auto-incrementing inventory identifier                    |
| EquipmentName      | NVARCHAR(200)  | NOT NULL              | Name of the equipment                                     |
| Description        | NVARCHAR(1000) | NULL                  | Detailed description                                      |
| Category           | NVARCHAR(100)  | NULL                  | Equipment category                                        |
| Brand              | NVARCHAR(100)  | NULL                  | Brand/manufacturer name                                   |
| Model              | NVARCHAR(100)  | NULL                  | Model number/name                                         |
| SerialNumber       | NVARCHAR(50)   | NULL, UNIQUE          | Serial number (where applicable)                          |
| Barcode            | NVARCHAR(50)   | NULL, UNIQUE          | Barcode for scanning                                      |
| ExpiryDate         | DATETIME       | NULL                  | Expiration date (if applicable)                           |
| ManufactureDate    | DATETIME       | NULL                  | Manufacture date                                          |
| PurchasePrice      | DECIMAL        | NULL, CHECK >= 0      | Purchase price                                            |
| Supplier           | NVARCHAR(200)  | NULL                  | Supplier name                                             |
| Quantity           | INTEGER        | NOT NULL, CHECK >= 0  | Total quantity owned                                      |
| AvailableQuantity  | INTEGER        | NOT NULL, CHECK >= 0  | Quantity available for assignment                         |
| Status             | NVARCHAR(50)   | NOT NULL              | Available, Assigned, Reserved, Expired, Damaged, Disposed |
| Location           | NVARCHAR(200)  | NULL                  | Storage location                                          |
| Notes              | NVARCHAR(1000) | NULL                  | Additional notes                                          |
| IsExpiryAlertSent  | BIT            | NOT NULL              | Prevents duplicate expiry alert notifications             |
| CreatedByUserId    | INTEGER        | NULL, FOREIGN KEY     | User who created the inventory item                       |
| CreatedAt          | DATETIME       | NOT NULL              | Record creation timestamp                                 |
| UpdatedAt          | DATETIME       | NULL                  | Last update timestamp                                     |
| CreatedBy          | NVARCHAR       | NULL                  | User/system that created the record                       |
| UpdatedBy          | NVARCHAR       | NULL                  | User/system that last updated the record                  |
| IsDeleted          | BIT            | NOT NULL              | Soft-delete flag                                          |
| DeletedAt          | DATETIME       | NULL                  | Soft-delete timestamp                                     |
| DeletedBy          | NVARCHAR       | NULL                  | User/system that soft-deleted the record                  |
| ConcurrencyToken   | TEXT           | NOT NULL              | EF concurrency token rotated on every insert/update       |

**Indexes:**

- Unique filtered indexes for `Barcode` and `SerialNumber` when present/non-deleted
- Category/status/date indexes for filtering and dashboard alerts

**Check Constraints:**

- Quantity and available quantity cannot be negative
- Purchase price cannot be negative
- Application layer enforces `0 <= AvailableQuantity <= Quantity`

---

### 3. InventoryAssignments Table

**Purpose**: Tracks equipment assignments to users with complete audit trail.

| Column             | Type           | Constraints           | Description                              |
| ------------------ | -------------- | --------------------- | ---------------------------------------- |
| Id                 | INTEGER        | PRIMARY KEY, IDENTITY | Auto-incrementing assignment identifier  |
| InventoryId        | INTEGER        | NOT NULL, FOREIGN KEY | Reference to Inventories.Id              |
| UserId             | INTEGER        | NOT NULL, FOREIGN KEY | Reference to recipient Users.Id          |
| AssignedQuantity   | INTEGER        | NOT NULL, CHECK > 0   | Quantity assigned                        |
| AssignedDate       | DATETIME       | NOT NULL              | When assignment was made                 |
| ReturnDate         | DATETIME       | NULL                  | When returned                            |
| ExpectedReturnDate | DATETIME       | NULL                  | Expected return date                     |
| Status             | NVARCHAR(50)   | NOT NULL              | Active, Returned, Expired, Lost, Damaged |
| AssignmentNotes    | NVARCHAR(1000) | NULL                  | Assignment notes                         |
| ReturnNotes        | NVARCHAR(1000) | NULL                  | Return notes                             |
| AssignedByUserId   | INTEGER        | NULL, FOREIGN KEY     | User who made the assignment             |
| ReturnedToUserId   | INTEGER        | NULL, FOREIGN KEY     | User who processed return                |
| CreatedAt          | DATETIME       | NOT NULL              | Record creation timestamp                |
| UpdatedAt          | DATETIME       | NULL                  | Last update timestamp                    |
| CreatedBy          | NVARCHAR       | NULL                  | User/system that created the record      |
| UpdatedBy          | NVARCHAR       | NULL                  | User/system that last updated the record |
| IsDeleted          | BIT            | NOT NULL              | Soft-delete flag                         |
| DeletedAt          | DATETIME       | NULL                  | Soft-delete timestamp                    |
| DeletedBy          | NVARCHAR       | NULL                  | User/system that soft-deleted the record |
| ConcurrencyToken   | TEXT           | NOT NULL              | EF concurrency token rotated on update   |

**Indexes:**

- `IX_InventoryAssignments_InventoryId`
- `IX_InventoryAssignments_UserId`
- `IX_InventoryAssignments_AssignedByUserId`
- `IX_InventoryAssignments_ReturnedToUserId`
- Status/date indexes for active, overdue, history, and recent queries

**Check Constraints:**

- `AssignedQuantity > 0`

---

### 4. IdempotentRequests Table

**Purpose**: Stores idempotency locks and cached responses for retry-safe mutating requests.

| Column              | Type           | Constraints             | Description                                             |
| ------------------- | -------------- | ----------------------- | ------------------------------------------------------- |
| IdempotencyKey      | NVARCHAR(512)  | PRIMARY KEY             | Client-supplied idempotency key                         |
| RequestMethod       | NVARCHAR(10)   | NOT NULL                | Original HTTP method                                    |
| RequestPath         | NVARCHAR(500)  | NOT NULL                | Original request path                                   |
| RequestHash         | NVARCHAR(64)   | NULL                    | SHA-256 hex hash of request body                        |
| ResponseStatusCode  | INTEGER        | NOT NULL                | Captured response status code; 0 while in progress      |
| ResponseBody        | NVARCHAR       | NULL, MAX 1048576 chars | Captured response body when cacheable                   |
| ResponseContentType | NVARCHAR(100)  | NULL                    | Captured content type                                   |
| CreatedAt           | DATETIME       | NOT NULL                | Lock creation timestamp                                 |
| CompletedAt         | DATETIME       | NULL                    | Response completion timestamp                           |
| LockExpiresAt       | DATETIME       | NOT NULL                | Abandoned-lock reclaim deadline                         |
| ExpiresAt           | DATETIME       | NULL                    | Completed response retention deadline                   |
| IsCompleted         | BIT            | NOT NULL                | Whether response was captured                           |

**Indexes:**

- `IX_IdempotentRequests_LockExpiresAt`
- `IX_IdempotentRequests_ExpiresAt`

The old `IX_IdempotentRequests_CreatedAt` index was dropped. The middleware's `Idempotency:MaxKeyLength` default is 100, even though the column allows up to 512.

---

## Relationships and Foreign Keys

### 1. InventoryAssignments → Inventories

```sql
FOREIGN KEY (InventoryId) REFERENCES Inventories(Id)
ON DELETE RESTRICT
```

- **Relationship**: Many-to-One
- **Navigation**: `InventoryAssignment.Inventory` ← `Inventory.Assignments`

### 2. InventoryAssignments → Users (Assigned User)

```sql
FOREIGN KEY (UserId) REFERENCES Users(Id)
ON DELETE RESTRICT
```

- **Relationship**: Many-to-One
- **Navigation**: `InventoryAssignment.User` ← `User.AssignedInventories`

### 3. InventoryAssignments → Users (Assigned By / Returned To)

```sql
FOREIGN KEY (AssignedByUserId) REFERENCES Users(Id) ON DELETE SET NULL
FOREIGN KEY (ReturnedToUserId) REFERENCES Users(Id) ON DELETE SET NULL
```

- **Relationship**: Many-to-One
- **Delete Behavior**: SET NULL where configured to preserve assignment records

### 4. Inventories → Users (Created By User)

```sql
FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
ON DELETE SET NULL
```

---

## Entity Relationship Diagram

```
┌─────────────────┐         ┌─────────────────────┐         ┌─────────────────┐
│     Users       │         │ InventoryAssignments│         │   Inventories   │
├─────────────────┤         ├─────────────────────┤         ├─────────────────┤
│ Id (PK)         │◄───────┤│ UserId (FK)         │├───────►│ Id (PK)         │
│ Email (UNIQUE)  │         │ AssignedByUserId(FK)│         │ EquipmentName   │
│ PasswordHash    │         │ ReturnedToUserId(FK)│         │ Category        │
│ Role/Flags      │         │ InventoryId (FK)    │         │ Brand           │
│ RefreshToken(*) │         │ AssignedQuantity    │         │ Serial/Barcode  │
│ IsDeleted       │         │ Status              │         │ Quantity        │
│ ConcurrencyToken│         │ ConcurrencyToken    │         │ AvailableQty    │
└─────────────────┘         └─────────────────────┘         │ ConcurrencyToken│
                                                            └─────────────────┘

(*) RefreshToken stores a SHA-256 hash, not the raw refresh token.

┌────────────────────────┐
│   IdempotentRequests   │
├────────────────────────┤
│ IdempotencyKey (PK)    │
│ RequestMethod/Path     │
│ RequestHash            │
│ ResponseStatusCode     │
│ CreatedAt/CompletedAt  │
│ LockExpiresAt/ExpiresAt│
│ IsCompleted            │
└────────────────────────┘
```

---

## Data Flow Patterns

### 1. User Registration Flow

```
Admin → Create User → Validate DTO → Hash Password (Microsoft PBKDF2) → Store Users row
```

### 2. Authentication Flow

```
Login → Verify password hash → Create access token using JwtOptions
      → Create raw refresh token → Store SHA-256 refresh-token hash
      → Return raw refresh token to client once
```

### 3. Assignment Flow

```
Admin/Provider → ExecuteInTransactionAsync → Validate inventory/user/quantity
                                           ↓
                                    Create InventoryAssignments record
                                           ↓
                                    Decrease AvailableQuantity
                                           ↓
                                    Save with ConcurrencyToken check
```

Concurrent conflicting updates raise a concurrency exception and return HTTP 409 instead of silently overselling; duplicate email/barcode/serial conflicts are also surfaced as 409 by the Result mapping layer.

### 4. Idempotency Flow

```
Mutating request + Idempotency-Key → Authenticate first → Hash request body
                                  ↓
       409 if in progress | 422 if key reused with different request
                                  ↓
                      Cache bounded response or stream too-large body
                                  ↓
                      Replay completed response with Idempotency-Replayed: true
```

---

## Business Rules Enforced by Database and Application

### 1. Data Integrity Rules

- **Email Uniqueness**: Each user must have a unique email address
- **Barcode/Serial Uniqueness**: Each non-deleted inventory item with a barcode/serial must be unique
- **Non-negative Quantities**: Inventory quantities cannot be negative
- **Positive Assignment Quantities**: Cannot assign zero or negative quantities
- **Concurrency Tokens**: Updates include the original `ConcurrencyToken`, preventing lost updates

### 2. Security Rules

- **PasswordHash** stores Microsoft PBKDF2 hashes only
- **RefreshToken** stores SHA-256 hashes only
- **JWT signing key** is not stored in the database and must be supplied through `Jwt` configuration/secrets
- **Auth idempotency exclusion** prevents token responses from being cached/replayed

### 3. Soft Delete Implementation

- **IsDeleted flag** on `BaseEntity` tables prevents hard deletes for domain entities
- **DeletedAt/DeletedBy** keep deletion audit data
- **Global query filters** exclude soft-deleted rows automatically

---

## Performance Considerations

### 1. Index Strategy

- **Primary Keys**: Fast entity lookup
- **Foreign Keys**: Join performance
- **Unique Constraints**: Email, barcode, serial-number checks
- **Status/Date Fields**: Common filtering for alerts and assignment history
- **Idempotency Expiry Indexes**: Efficient stale-lock and retention cleanup

### 2. Query Optimization Patterns

- **AsNoTracking Reads**: Repositories default to no tracking for read paths
- **Deterministic Pagination**: Mandatory `OrderBy` before `Skip/Take`
- **SQL-Side Filtering**: Search, ordering, paging, and `Take` are pushed into EF queries
- **Sequential Dashboard Aggregates**: Avoids concurrent operations on one scoped DbContext

---

## Migration and Versioning

### 1. Current Security/Architecture Migration

- **Migration**: `SecurityAndConcurrencyHardening`
- **Added**: `ConcurrencyToken` to Users, Inventories, InventoryAssignments
- **Added**: `RequestHash`, `CompletedAt`, `LockExpiresAt`, `ExpiresAt` to IdempotentRequests
- **Added Indexes**: `IX_IdempotentRequests_LockExpiresAt`, `IX_IdempotentRequests_ExpiresAt`
- **Configured Limits**: `IdempotencyKey` max 512 and `ResponseBody` max 1048576 characters at the EF model layer
- **Dropped Index**: `IX_IdempotentRequests_CreatedAt`

### 2. Migration Commands

```powershell
# Add new migration
dotnet ef migrations add MigrationName --project src\InventoryManagement.Infrastructure --startup-project src\InventoryManagement.API

# Update database
dotnet ef database update --project src\InventoryManagement.Infrastructure --startup-project src\InventoryManagement.API

# Generate SQL script
dotnet ef migrations script --project src\InventoryManagement.Infrastructure --startup-project src\InventoryManagement.API
```

### 3. Database Backup/Restore

```powershell
Copy-Item src\InventoryManagement.Infrastructure\inventory.db backup\inventory_backup_20260813.db
Copy-Item backup\inventory_backup_20260813.db src\InventoryManagement.Infrastructure\inventory.db
```

---

## Monitoring and Maintenance

### 1. Database Statistics

- **Record Counts**: Monitor growth patterns
- **Idempotency Rows**: Cleanup service purges expired rows in bounded batches
- **Query Performance**: Log slow queries via Serilog/EF logging configuration

### 2. Maintenance Tasks

- **Cleanup Expired Idempotency Records**: Automated by `IdempotencyCleanupService`
- **Refresh Token Hygiene**: Logout and password change revoke current refresh tokens
- **Backup Schedule**: Regular automated backups

### 3. Health Checks

```http
GET /health
```

This database schema provides a robust foundation with relationships, optimistic concurrency, idempotency safety, and performance optimizations.
