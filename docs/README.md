# Inventory Management System - Documentation

## 📋 Overview

Welcome to the comprehensive documentation for the **Inventory Management System** - a robust solution for managing medical equipment inventory with expiry tracking, assignment management, and real-time visibility designed specifically for healthcare environments with Nurse Practitioners.

## 🗂️ Documentation Structure

### 1. [API Testing Guide](./API-Testing-Guide.md) 📡

**Complete step-by-step testing documentation for all API endpoints**

- **Pre-seeded Admin Access**: Default credentials and initial setup
- **Controller-by-Controller Testing**: Detailed flows for Auth, Users, Inventory, Assignments, and Dashboard
- **Business Logic Testing**: Proper sequences to avoid errors and exceptions
- **Error Scenario Testing**: Authentication, authorization, validation, and business rule violations
- **Testing Tools**: PowerShell, cURL, and Postman examples
- **Data Management**: Reset procedures and test data sequences

**🎯 Use this when**: You need to test the API endpoints systematically without encountering errors.

---

### 2. [Database Schema](./Database-Schema.md) 🗄️

**Comprehensive database design and relationship documentation**

- **Table Structures**: Complete schema for Users, Inventories, and InventoryAssignments
- **Relationships & Foreign Keys**: Entity relationships with proper constraints
- **Indexes & Performance**: Optimized indexes for common queries
- **Business Rules**: Database-enforced integrity and validation rules
- **Migration Management**: EF Core migration commands and versioning
- **Performance Considerations**: Query optimization and connection management

**🎯 Use this when**: You need to understand the database structure, relationships, or plan schema modifications.

---

### 3. [Domain Model](./Domain-Model.md) 🏗️

**Domain-driven design documentation covering entities, DTOs, and business logic**

- **Core Entities**: User, Inventory, InventoryAssignment with business methods
- **Enums & Constants**: UserRole, InventoryStatus, AssignmentStatus with business meanings
- **DTOs & Validation**: Complete request/response models with validation attributes
- **Repository Interfaces**: Data access contracts and specialized methods
- **Service Interfaces**: Business logic contracts and operation definitions
- **Domain Relationships**: Entity navigation properties and aggregate boundaries

**🎯 Use this when**: You need to understand the business domain, extend functionality, or modify business rules.

---

### 4. [Data Flow & Architecture](./Data-Flow-Architecture.md) 🔄

**System architecture and data flow patterns throughout the application layers**

- **Clean Architecture Layers**: Presentation, Application, Domain, and Infrastructure separation
- **Request/Response Patterns**: Authentication, authorization, and business logic flows
- **Data Transformation**: Entity ↔ DTO mapping patterns and audit trail implementation
- **Transaction Management**: Unit of Work pattern and business consistency
- **Error Handling**: Comprehensive error pipeline and standardized responses
- **Performance Patterns**: Caching strategies, pagination, and query optimization

**🎯 Use this when**: You need to understand how data moves through the system or implement new features.

---

## 🚀 Quick Start Guide

### Prerequisites Setup

1. **Install .NET 10 SDK** (if not already installed)
2. **Trust HTTPS certificates** for development:
   ```powershell
   dotnet dev-certs https --trust
   ```

### Project Setup & Running

#### Option 1: Quick Start (from solution root)

```powershell
# Navigate to project root
cd "D:\Temp Files\InventoryManagement"

# Restore all packages for the solution
dotnet restore

# Navigate to API project
cd src\InventoryManagement.API

# Build the project
dotnet build

# Run the API (HTTPS development profile)
dotnet run --launch-profile https
```

#### Option 2: Step-by-Step Setup

```powershell
# 1. Navigate to API directory
cd "D:\Temp Files\InventoryManagement\src\InventoryManagement.API"

# 2. Restore NuGet packages
dotnet restore

# 3. Build the project to check for errors
dotnet build

# 4. Run on HTTPS (recommended for JWT tokens)
dotnet run --launch-profile https

# Alternative: Run with specific URLs
dotnet run --urls="https://localhost:7178;http://localhost:5067"
```

#### Option 3: Using Visual Studio

1. Open `InventoryManagement.sln` in Visual Studio
2. Set `InventoryManagement.API` as startup project
3. Select **HTTPS** launch profile
4. Press **F5** or click **Start Debugging**

### After Startup

1. **API Available at**: `https://localhost:7178`
2. **Swagger/OpenAPI**: `https://localhost:7178/scalar/v1`
3. **Default Admin Login**:
   - **Email**: `admin@inventorymanagement.com`
   - **Password**: `Admin@123`

### For API Testing

1. **Access Scalar API Documentation**: Navigate to `https://localhost:7178/scalar/v1`
2. **Login as Admin**: Use default credentials above
3. **Copy JWT Token**: From login response for authenticated endpoints
4. **Follow Test Sequences**: Detailed steps in [API Testing Guide](./API-Testing-Guide.md)

### For Development

1. **Review Domain Model**: Understand entities and business rules
2. **Examine Database Schema**: Learn data structure and relationships
3. **Study Data Flow**: Understand request processing and layer interactions
4. **Test APIs**: Validate your changes using the testing guide

### Troubleshooting Startup

#### SSL Certificate Issues

```powershell
# Clean and reinstall development certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

#### Port Conflicts

```powershell
# Check what's using port 7178
netstat -ano | findstr :7178

# Run on alternative ports
dotnet run --urls="https://localhost:7179;http://localhost:5068"
```

#### Package Restore Issues

```powershell
# Clear NuGet cache and restore
dotnet nuget locals all --clear
dotnet restore --force
```

#### Database Issues

- Database auto-creates on first run
- Location: `src/InventoryManagement.Infrastructure/inventory.db`
- To reset: Stop app, delete database file, restart app

---

## 🏥 Business Context

### System Purpose

This inventory management system addresses the specific needs of healthcare offices that:

- **Receive medical equipment** for distribution to Nurse Practitioners
- **Track expiry dates** with 3-6 month advance alerts for redistribution
- **Manage assignments** to individual practitioners with return tracking
- **Maintain real-time visibility** of inventory status and availability
- **Support barcode scanning** for efficient inventory operations

### Key Features Documented

#### 🔐 Security & Authentication

- **JWT-based authentication** with refresh tokens
- **Role-based authorization** (Admin, NursePractitioner, Staff)
- **Policy-based access control** for granular permissions
- **Comprehensive audit trails** for all operations

#### 📦 Inventory Management

- **Complete CRUD operations** with validation
- **Barcode scanning support** for quick identification
- **Expiry date tracking** with configurable alerts (3-6 months)
- **Low stock monitoring** with customizable thresholds
- **Category and manufacturer organization**

#### 👥 Assignment Workflow

- **Equipment assignment** to Nurse Practitioners
- **Return processing** with condition tracking
- **Overdue assignment monitoring** with alerts
- **Complete assignment history** for audit purposes
- **Quantity tracking** with partial return support

#### 📊 Dashboard & Reporting

- **Real-time statistics** for inventory and assignments
- **Multiple alert types** (expiry, low stock, overdue)
- **Recent activity feeds** for operational awareness
- **Performance-optimized** concurrent data loading

---

## 🛠️ Technical Architecture

### Clean Architecture Implementation

```
API Layer (Controllers)
    ↓
Application Layer (Services)
    ↓
Domain Layer (Entities & Business Logic)
    ↓
Infrastructure Layer (Database & External Services)
```

### Technology Stack

- **Framework**: ASP.NET Core 9.0
- **Database**: SQLite with Entity Framework Core 9.0
- **Authentication**: JWT Bearer tokens
- **Logging**: Serilog with file rotation
- **API Documentation**: OpenAPI with Scalar
- **Architecture**: Clean Architecture with DDD principles

---

## 📚 API Endpoint Summary

### Authentication (`/api/auth`)

- `POST /login` - User authentication
- `POST /refresh` - Token refresh
- `POST /logout` - User logout
- `POST /change-password` - Password change
- `GET /me` - Current user info

### Users (`/api/users`)

- `GET /` - All users (Admin only)
- `POST /` - Create user (Admin only)
- `GET /{id}` - User details (Admin or own profile)
- `PUT /{id}` - Update user (Admin or own profile)
- `DELETE /{id}` - Delete user (Admin only)

### Inventory (`/api/inventory`)

- `GET /` - All inventory items
- `POST /` - Create inventory (Admin/Provider)
- `GET /{id}` - Item details
- `GET /barcode/{barcode}` - Barcode lookup
- `POST /search` - Advanced search
- `GET /expiring` - Expiry alerts
- `GET /low-stock` - Low stock alerts

### Assignments (`/api/inventoryassignments`)

- `GET /` - All assignments (Admin/Provider)
- `POST /` - Create assignment (Admin/Provider)
- `GET /my-assignments` - Current user's assignments
- `POST /return` - Process return (Admin/Provider)
- `GET /overdue` - Overdue assignments
- `GET /history/inventory/{id}` - Assignment history

### Dashboard (`/api/dashboard`)

- `GET /stats` - System statistics
- `GET /overview` - Complete dashboard data
- `GET /alerts/summary` - All alerts summary
- `GET /recent-inventories` - Recent items
- `GET /recent-assignments` - Recent assignments

---

## 🔍 Common Use Cases

### Testing New Features

1. **Read**: [Domain Model](./Domain-Model.md) for business rules
2. **Plan**: Check [Database Schema](./Database-Schema.md) for data requirements
3. **Implement**: Follow [Data Flow](./Data-Flow-Architecture.md) patterns
4. **Test**: Use [API Testing Guide](./API-Testing-Guide.md) sequences

### Troubleshooting Issues

1. **API Errors**: Check [API Testing Guide](./API-Testing-Guide.md) error scenarios
2. **Database Issues**: Review [Database Schema](./Database-Schema.md) constraints
3. **Business Logic**: Examine [Domain Model](./Domain-Model.md) validation rules
4. **Flow Problems**: Study [Data Flow](./Data-Flow-Architecture.md) patterns

### Adding New Endpoints

1. **Domain**: Add entities/DTOs in [Domain Model](./Domain-Model.md)
2. **Database**: Update schema per [Database Schema](./Database-Schema.md)
3. **Implementation**: Follow [Data Flow](./Data-Flow-Architecture.md) patterns
4. **Testing**: Create test cases per [API Testing Guide](./API-Testing-Guide.md)

---

## 📞 Support Information

### Default Admin Access

- **Email**: `admin@inventorymanagement.com`
- **Password**: `Admin@123`
- **Capabilities**: Full system access for initial setup

### Database Reset (Development)

1. Stop the application
2. Delete `src/InventoryManagement.Infrastructure/inventory.db`
3. Restart the application (auto-recreates with admin user)

### Logging Location

- **Log Files**: `src/InventoryManagement.API/logs/`
- **Rotation**: Hourly with 7-day retention
- **Format**: Structured JSON with user context

---

## 🎯 Next Steps

### For Developers

1. **Start with**: [Domain Model](./Domain-Model.md) to understand business concepts
2. **Then explore**: [Database Schema](./Database-Schema.md) for data structure
3. **Understand flow**: [Data Flow](./Data-Flow-Architecture.md) for implementation patterns
4. **Test thoroughly**: [API Testing Guide](./API-Testing-Guide.md) for validation

### For Testers

1. **Begin with**: [API Testing Guide](./API-Testing-Guide.md) for step-by-step procedures
2. **Reference**: [Domain Model](./Domain-Model.md) for business rule validation
3. **Verify data**: [Database Schema](./Database-Schema.md) for data integrity checks

### For System Administrators

1. **Review**: [Database Schema](./Database-Schema.md) for backup and maintenance
2. **Monitor**: [Data Flow](./Data-Flow-Architecture.md) for performance optimization
3. **Maintain**: [API Testing Guide](./API-Testing-Guide.md) for health checks

---

## 📋 Documentation Checklist

- ✅ **API Testing Guide**: Complete endpoint testing with error scenarios
- ✅ **Database Schema**: Full table structures with relationships and constraints
- ✅ **Domain Model**: Comprehensive business entities and DTOs
- ✅ **Data Flow & Architecture**: System patterns and implementation guidance
- ✅ **Overview Documentation**: Navigation and quick start guide

All documentation is current as of the latest system implementation and covers the complete inventory management workflow from user authentication through equipment assignment and return processing.

---

_This documentation is maintained alongside the codebase and should be updated when business rules, API endpoints, or data structures change._
