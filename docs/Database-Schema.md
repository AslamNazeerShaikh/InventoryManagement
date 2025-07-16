# Database Schema Documentation - Inventory Management System

## Overview

This document provides a comprehensive overview of the database schema, including table structures, relationships, constraints, and data flow patterns.

## Database Technology

- **Database Engine**: SQLite
- **File Location**: `src/InventoryManagement.Infrastructure/inventory.db`
- **ORM**: Entity Framework Core 9.0
- **Migration Support**: Yes (Code-First approach)

---

## Table Structures

### 1. Users Table

**Purpose**: Stores user accounts with role-based access control

| Column                 | Type          | Constraints           | Description                             |
| ---------------------- | ------------- | --------------------- | --------------------------------------- |
| Id                     | INTEGER       | PRIMARY KEY, IDENTITY | Auto-incrementing user identifier       |
| Email                  | NVARCHAR(256) | NOT NULL, UNIQUE      | User's email address (login identifier) |
| PasswordHash           | NVARCHAR(500) | NOT NULL              | BCrypt hashed password                  |
| Name                   | NVARCHAR(200) | NOT NULL              | Full name of the user                   |
| Role                   | NVARCHAR(50)  | NOT NULL              | Role: Admin, NursePractitioner, Staff   |
| IsAdmin                | BIT           | NOT NULL              | Quick admin check flag                  |
| IsProvider             | BIT           | NOT NULL              | Quick provider (NP) check flag          |
| IsActive               | BIT           | NOT NULL              | Soft delete flag                        |
| RefreshToken           | NVARCHAR(500) | NULL                  | JWT refresh token                       |
| RefreshTokenExpiryTime | DATETIME2     | NULL                  | Refresh token expiration                |
| CreatedAt              | DATETIME2     | NOT NULL              | Record creation timestamp               |
| UpdatedAt              | DATETIME2     | NOT NULL              | Last update timestamp                   |
| CreatedBy              | NVARCHAR(100) | NULL                  | User who created the record             |
| UpdatedBy              | NVARCHAR(100) | NULL                  | User who last updated the record        |

**Indexes:**

- `IX_Users_Email` (UNIQUE)
- `IX_Users_Role`
- `IX_Users_IsActive`

**Sample Data:**

```sql
INSERT INTO Users (Email, PasswordHash, Name, Role, IsAdmin, IsProvider, IsActive, CreatedAt, UpdatedAt)
VALUES ('admin@inventorymanagement.com', '$2a$11$hash...', 'System Administrator', 'Admin', 1, 0, 1, GETDATE(), GETDATE());
```

---

### 2. Inventories Table

**Purpose**: Stores medical equipment and supplies inventory

| Column        | Type           | Constraints           | Description                                               |
| ------------- | -------------- | --------------------- | --------------------------------------------------------- |
| Id            | INTEGER        | PRIMARY KEY, IDENTITY | Auto-incrementing inventory identifier                    |
| EquipmentName | NVARCHAR(200)  | NOT NULL              | Name of the equipment                                     |
| Description   | NVARCHAR(1000) | NULL                  | Detailed description                                      |
| Category      | NVARCHAR(100)  | NOT NULL              | Equipment category                                        |
| Manufacturer  | NVARCHAR(100)  | NULL                  | Manufacturer name                                         |
| Model         | NVARCHAR(100)  | NULL                  | Model number/name                                         |
| SerialNumber  | NVARCHAR(100)  | NULL, UNIQUE          | Serial number (where applicable)                          |
| Barcode       | NVARCHAR(50)   | NULL, UNIQUE          | Barcode for scanning                                      |
| Quantity      | INTEGER        | NOT NULL, CHECK >= 0  | Current quantity in stock                                 |
| UnitCost      | DECIMAL(18,2)  | NOT NULL, CHECK >= 0  | Cost per unit                                             |
| ExpiryDate    | DATETIME2      | NULL                  | Expiration date (if applicable)                           |
| Status        | NVARCHAR(50)   | NOT NULL              | Available, Assigned, Reserved, Expired, Damaged, Disposed |
| Location      | NVARCHAR(200)  | NULL                  | Storage location                                          |
| Notes         | NVARCHAR(1000) | NULL                  | Additional notes                                          |
| IsActive      | BIT            | NOT NULL              | Soft delete flag                                          |
| CreatedAt     | DATETIME2      | NOT NULL              | Record creation timestamp                                 |
| UpdatedAt     | DATETIME2      | NOT NULL              | Last update timestamp                                     |
| CreatedBy     | NVARCHAR(100)  | NULL                  | User who created the record                               |
| UpdatedBy     | NVARCHAR(100)  | NULL                  | User who last updated the record                          |

**Indexes:**

- `IX_Inventories_Barcode` (UNIQUE, WHERE Barcode IS NOT NULL)
- `IX_Inventories_SerialNumber` (UNIQUE, WHERE SerialNumber IS NOT NULL)
- `IX_Inventories_Category`
- `IX_Inventories_Status`
- `IX_Inventories_ExpiryDate`
- `IX_Inventories_IsActive`

**Check Constraints:**

- `CK_Inventories_Quantity_NonNegative`: Quantity >= 0
- `CK_Inventories_UnitCost_NonNegative`: UnitCost >= 0

---

### 3. InventoryAssignments Table

**Purpose**: Tracks equipment assignments to users with complete audit trail

| Column             | Type           | Constraints           | Description                              |
| ------------------ | -------------- | --------------------- | ---------------------------------------- |
| Id                 | INTEGER        | PRIMARY KEY, IDENTITY | Auto-incrementing assignment identifier  |
| InventoryId        | INTEGER        | NOT NULL, FOREIGN KEY | Reference to Inventories.Id              |
| UserId             | INTEGER        | NOT NULL, FOREIGN KEY | Reference to Users.Id                    |
| AssignedByUserId   | INTEGER        | NOT NULL, FOREIGN KEY | User who made the assignment             |
| QuantityAssigned   | INTEGER        | NOT NULL, CHECK > 0   | Quantity assigned                        |
| AssignedDate       | DATETIME2      | NOT NULL              | When assignment was made                 |
| ExpectedReturnDate | DATETIME2      | NULL                  | Expected return date                     |
| Status             | NVARCHAR(50)   | NOT NULL              | Active, Returned, Expired, Lost, Damaged |
| Notes              | NVARCHAR(1000) | NULL                  | Assignment notes                         |
| QuantityReturned   | INTEGER        | NULL, CHECK >= 0      | Quantity returned (when applicable)      |
| ActualReturnDate   | DATETIME2      | NULL                  | When actually returned                   |
| ReturnedToUserId   | INTEGER        | NULL, FOREIGN KEY     | User who processed return                |
| ReturnCondition    | NVARCHAR(100)  | NULL                  | Condition when returned                  |
| ReturnNotes        | NVARCHAR(1000) | NULL                  | Return notes                             |
| IsActive           | BIT            | NOT NULL              | Soft delete flag                         |
| CreatedAt          | DATETIME2      | NOT NULL              | Record creation timestamp                |
| UpdatedAt          | DATETIME2      | NOT NULL              | Last update timestamp                    |
| CreatedBy          | NVARCHAR(100)  | NULL                  | User who created the record              |
| UpdatedBy          | NVARCHAR(100)  | NULL                  | User who last updated the record         |

**Indexes:**

- `IX_InventoryAssignments_InventoryId`
- `IX_InventoryAssignments_UserId`
- `IX_InventoryAssignments_AssignedByUserId`
- `IX_InventoryAssignments_ReturnedToUserId`
- `IX_InventoryAssignments_Status`
- `IX_InventoryAssignments_AssignedDate`
- `IX_InventoryAssignments_ExpectedReturnDate`
- `IX_InventoryAssignments_IsActive`

**Check Constraints:**

- `CK_InventoryAssignments_QuantityAssigned_Positive`: QuantityAssigned > 0
- `CK_InventoryAssignments_QuantityReturned_NonNegative`: QuantityReturned >= 0

---

## Relationships and Foreign Keys

### 1. InventoryAssignments → Inventories

```sql
FOREIGN KEY (InventoryId) REFERENCES Inventories(Id)
ON DELETE RESTRICT
```

- **Relationship**: Many-to-One (Many assignments can reference one inventory item)
- **Delete Behavior**: RESTRICT (Cannot delete inventory with active assignments)
- **Navigation**: `InventoryAssignment.Inventory` ← `Inventory.Assignments`

### 2. InventoryAssignments → Users (Assigned User)

```sql
FOREIGN KEY (UserId) REFERENCES Users(Id)
ON DELETE RESTRICT
```

- **Relationship**: Many-to-One (Many assignments can be made to one user)
- **Delete Behavior**: RESTRICT (Cannot delete user with active assignments)
- **Navigation**: `InventoryAssignment.User` ← `User.Assignments`

### 3. InventoryAssignments → Users (Assigned By User)

```sql
FOREIGN KEY (AssignedByUserId) REFERENCES Users(Id)
ON DELETE RESTRICT
```

- **Relationship**: Many-to-One (Many assignments can be made by one user)
- **Delete Behavior**: RESTRICT (Maintains audit trail)
- **Navigation**: `InventoryAssignment.AssignedByUser` ← `User.AssignmentsMade`

### 4. InventoryAssignments → Users (Returned To User)

```sql
FOREIGN KEY (ReturnedToUserId) REFERENCES Users(Id)
ON DELETE SET NULL
```

- **Relationship**: Many-to-One (Many returns can be processed by one user)
- **Delete Behavior**: SET NULL (Return processing record preserved)
- **Navigation**: `InventoryAssignment.ReturnedToUser` ← `User.ReturnsProcessed`

---

## Entity Relationship Diagram

```
┌─────────────────┐         ┌─────────────────────┐         ┌─────────────────┐
│     Users       │         │ InventoryAssignments│         │   Inventories   │
├─────────────────┤         ├─────────────────────┤         ├─────────────────┤
│ Id (PK)         │◄───────┤│ UserId (FK)         │├───────►│ Id (PK)         │
│ Email (UNIQUE)  │         │ AssignedByUserId(FK)│         │ EquipmentName   │
│ PasswordHash    │         │ ReturnedToUserId(FK)│         │ Description     │
│ Name            │         │ InventoryId (FK)    │         │ Category        │
│ Role            │         │ QuantityAssigned    │         │ Manufacturer    │
│ IsAdmin         │         │ AssignedDate        │         │ Model           │
│ IsProvider      │         │ ExpectedReturnDate  │         │ SerialNumber    │
│ IsActive        │         │ Status              │         │ Barcode (UNIQUE)│
│ RefreshToken    │         │ QuantityReturned    │         │ Quantity        │
│ RefreshTokenExp │         │ ActualReturnDate    │         │ UnitCost        │
│ CreatedAt       │         │ ReturnCondition     │         │ ExpiryDate      │
│ UpdatedAt       │         │ Notes               │         │ Status          │
│ CreatedBy       │         │ ReturnNotes         │         │ Location        │
│ UpdatedBy       │         │ IsActive            │         │ Notes           │
└─────────────────┘         │ CreatedAt           │         │ IsActive        │
                            │ UpdatedAt           │         │ CreatedAt       │
                            │ CreatedBy           │         │ UpdatedAt       │
                            │ UpdatedBy           │         │ CreatedBy       │
                            └─────────────────────┘         │ UpdatedBy       │
                                                            └─────────────────┘

Relationships:
1. Users(Id) ← InventoryAssignments(UserId) [1:M]
2. Users(Id) ← InventoryAssignments(AssignedByUserId) [1:M]
3. Users(Id) ← InventoryAssignments(ReturnedToUserId) [1:M]
4. Inventories(Id) ← InventoryAssignments(InventoryId) [1:M]
```

---

## Data Flow Patterns

### 1. User Registration Flow

```
Admin → Create User → Hash Password → Store in Users table
                   ↓
            Set appropriate Role/IsAdmin/IsProvider flags
```

### 2. Inventory Creation Flow

```
Admin/Provider → Create Inventory → Validate uniqueness (Barcode/Serial)
                                  ↓
                            Store in Inventories table
                                  ↓
                            Set Status = "Available"
```

### 3. Assignment Flow

```
Admin/Provider → Create Assignment → Validate quantity availability
                                   ↓
                            Check inventory status != "Expired"
                                   ↓
                            Create InventoryAssignments record
                                   ↓
                            Update Inventory quantity (if tracking)
                                   ↓
                            Set Assignment status = "Active"
```

### 4. Return Flow

```
Admin/Provider → Process Return → Validate assignment exists
                                ↓
                        Update InventoryAssignments:
                        - QuantityReturned
                        - ActualReturnDate
                        - ReturnCondition
                        - Status = "Returned"
                                ↓
                        Update Inventory quantity (add back)
```

### 5. Audit Trail Flow

```
Any Entity Update → Update audit fields:
                   - UpdatedAt = GETDATE()
                   - UpdatedBy = Current User
                   ↓
            Maintain complete history via timestamps
```

---

## Business Rules Enforced by Database

### 1. Data Integrity Rules

- **Email Uniqueness**: Each user must have a unique email address
- **Barcode Uniqueness**: Each inventory item with a barcode must be unique
- **Serial Number Uniqueness**: Each inventory item with a serial number must be unique
- **Non-negative Quantities**: Inventory quantities cannot be negative
- **Positive Assignment Quantities**: Cannot assign zero or negative quantities

### 2. Referential Integrity Rules

- **Cannot delete users with active assignments**
- **Cannot delete inventory items with active assignments**
- **Assignment must reference valid inventory and user**
- **Assignment audit trail preserved (AssignedBy/ReturnedTo users)**

### 3. Soft Delete Implementation

- **IsActive flag** on all tables prevents hard deletes
- **Maintains historical data** for auditing and reporting
- **Filters in queries** automatically exclude inactive records

---

## Performance Considerations

### 1. Index Strategy

- **Primary Keys**: Clustered indexes for fast lookups
- **Foreign Keys**: Non-clustered indexes for join performance
- **Unique Constraints**: Automatic indexes for uniqueness checks
- **Status Fields**: Indexes for common filtering operations
- **Date Fields**: Indexes for date range queries (expiry alerts)

### 2. Query Optimization Patterns

- **Pagination**: Uses OFFSET/FETCH for large result sets
- **Filtered Indexes**: Unique constraints with WHERE clauses
- **Selective Queries**: Always include IsActive = 1 filter
- **Date Range Queries**: Optimized for expiry and assignment date filtering

### 3. Connection Management

- **DbContext Scoped**: Per request in API
- **Connection Pooling**: Automatic via Entity Framework
- **Transaction Management**: Unit of Work pattern for consistency

---

## Migration and Versioning

### 1. Current Migration

- **Initial Migration**: Creates all tables with relationships
- **Seed Data**: Admin user creation
- **Indexes**: All performance indexes created

### 2. Migration Commands

```bash
# Add new migration
dotnet ef migrations add MigrationName --project src/InventoryManagement.Infrastructure

# Update database
dotnet ef database update --project src/InventoryManagement.Infrastructure

# Generate SQL script
dotnet ef migrations script --project src/InventoryManagement.Infrastructure
```

### 3. Database Backup/Restore

```bash
# Backup SQLite database
cp src/InventoryManagement.Infrastructure/inventory.db backup/inventory_backup_$(date +%Y%m%d).db

# Restore from backup
cp backup/inventory_backup_20240101.db src/InventoryManagement.Infrastructure/inventory.db
```

---

## Monitoring and Maintenance

### 1. Database Statistics

- **Record Counts**: Monitor growth patterns
- **Index Usage**: Identify unused indexes
- **Query Performance**: Log slow queries via Serilog
- **Integrity Checks**: Regular constraint validation

### 2. Maintenance Tasks

- **Cleanup Old Refresh Tokens**: Remove expired tokens
- **Archive Completed Assignments**: Move old assignments to archive table
- **Reindex Operations**: Rebuild indexes if performance degrades
- **Backup Schedule**: Regular automated backups

### 3. Health Checks

```csharp
// Connection health check
services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();
```

This database schema provides a robust foundation for the inventory management system with proper relationships, constraints, and performance optimizations.
