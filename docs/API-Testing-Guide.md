# API Testing Guide - Inventory Management System

## Overview

This guide provides step-by-step instructions for testing all API endpoints in the correct sequence to ensure proper data flow and avoid exceptions.

## Base URL

- **Development**: `https://localhost:7178`
- **API Documentation**: `https://localhost:7178/openapi/v1.json`

## Authentication Setup

### Step 1: Initial Admin Login

The system comes pre-seeded with an admin user for initial access.

**Default Admin Credentials:**

- Email: `admin@inventorymanagement.com`
- Password: `Admin@123`

**Request:**

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@inventorymanagement.com",
  "password": "Admin@123"
}
```

**Expected Response:**

```json
{
  "isSuccess": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "expiresAt": "2024-01-01T12:00:00Z",
    "user": {
      "id": 1,
      "email": "admin@inventorymanagement.com",
      "name": "System Administrator",
      "role": "Admin",
      "isAdmin": true,
      "isProvider": false
    }
  },
  "message": "Login successful"
}
```

**Save the JWT token** - you'll need it for all subsequent requests as `Authorization: Bearer {token}`

---

## Testing Flow by Controller

## 1. AuthController Testing

### 1.1 Get Current User Info

```http
GET /api/auth/me
Authorization: Bearer {your_jwt_token}
```

### 1.2 Change Password

```http
POST /api/auth/change-password
Authorization: Bearer {your_jwt_token}
Content-Type: application/json

{
  "currentPassword": "Admin@123",
  "newPassword": "NewPassword@123",
  "confirmPassword": "NewPassword@123"
}
```

### 1.3 Refresh Token

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "your_refresh_token_here"
}
```

### 1.4 Logout

```http
POST /api/auth/logout
Authorization: Bearer {your_jwt_token}
```

---

## 2. UsersController Testing

### 2.1 Create Users (Admin Only)

#### Create a Nurse Practitioner

```http
POST /api/users
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "email": "nurse1@hospital.com",
  "password": "Nurse@123",
  "name": "Dr. Sarah Johnson",
  "role": "NursePractitioner",
  "isAdmin": false,
  "isProvider": true
}
```

#### Create Staff Member

```http
POST /api/users
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "email": "staff1@hospital.com",
  "password": "Staff@123",
  "name": "John Smith",
  "role": "Staff",
  "isAdmin": false,
  "isProvider": false
}
```

### 2.2 Get All Users

```http
GET /api/users
Authorization: Bearer {admin_jwt_token}
```

### 2.3 Get Users with Pagination

```http
GET /api/users/paged?pageNumber=1&pageSize=10
Authorization: Bearer {admin_jwt_token}
```

### 2.4 Get User by ID

```http
GET /api/users/1
Authorization: Bearer {admin_jwt_token}
```

### 2.5 Get User by Email

```http
GET /api/users/by-email/nurse1@hospital.com
Authorization: Bearer {admin_jwt_token}
```

### 2.6 Update User

```http
PUT /api/users/2
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "email": "nurse1@hospital.com",
  "name": "Dr. Sarah Johnson-Updated",
  "role": "NursePractitioner",
  "isAdmin": false,
  "isProvider": true
}
```

### 2.7 Get Nurse Practitioners

```http
GET /api/users/nurse-practitioners
Authorization: Bearer {admin_jwt_token}
```

### 2.8 Get Active Users

```http
GET /api/users/active
Authorization: Bearer {admin_jwt_token}
```

---

## 3. InventoryController Testing

### 3.1 Create Inventory Items

#### Create Medical Equipment 1

```http
POST /api/inventory
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "equipmentName": "Digital Thermometer",
  "description": "Non-contact infrared thermometer",
  "category": "Diagnostic Equipment",
  "manufacturer": "MedTech Corp",
  "model": "MT-2024",
  "serialNumber": "MT2024001",
  "barcode": "1234567890123",
  "quantity": 25,
  "unitCost": 89.99,
  "expiryDate": "2026-12-31T00:00:00Z",
  "location": "Storage Room A",
  "notes": "Temperature range: -10°C to 50°C"
}
```

#### Create Medical Equipment 2

```http
POST /api/inventory
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "equipmentName": "Blood Pressure Monitor",
  "description": "Automatic digital blood pressure monitor",
  "category": "Diagnostic Equipment",
  "manufacturer": "HealthTech Inc",
  "model": "HT-BP200",
  "serialNumber": "HT200001",
  "barcode": "2345678901234",
  "quantity": 15,
  "unitCost": 149.99,
  "expiryDate": "2027-06-30T00:00:00Z",
  "location": "Storage Room B",
  "notes": "Adult and pediatric cuffs included"
}
```

#### Create Expiring Item (for testing alerts)

```http
POST /api/inventory
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "equipmentName": "Disposable Syringes",
  "description": "Sterile disposable syringes 10ml",
  "category": "Disposables",
  "manufacturer": "SafeMed",
  "model": "SM-SYR10",
  "serialNumber": "SM2024001",
  "barcode": "3456789012345",
  "quantity": 3,
  "unitCost": 2.50,
  "expiryDate": "2025-03-15T00:00:00Z",
  "location": "Storage Room C",
  "notes": "Low stock - expiring soon"
}
```

### 3.2 Retrieve Inventory Items

#### Get All Inventory

```http
GET /api/inventory
Authorization: Bearer {admin_jwt_token}
```

#### Get Paginated Inventory

```http
GET /api/inventory/paged?pageNumber=1&pageSize=5
Authorization: Bearer {admin_jwt_token}
```

#### Get Item by ID

```http
GET /api/inventory/1
Authorization: Bearer {admin_jwt_token}
```

#### Get Item by Barcode (Scanning Simulation)

```http
GET /api/inventory/barcode/1234567890123
Authorization: Bearer {admin_jwt_token}
```

### 3.3 Search and Filter

#### Search Inventory

```http
POST /api/inventory/search
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "searchTerm": "thermometer",
  "category": "Diagnostic Equipment",
  "manufacturer": "MedTech Corp"
}
```

#### Get Available Items

```http
GET /api/inventory/available
Authorization: Bearer {admin_jwt_token}
```

#### Get Expiring Items (3-6 months)

```http
GET /api/inventory/expiring?monthsBefore=6
Authorization: Bearer {admin_jwt_token}
```

#### Get Low Stock Items

```http
GET /api/inventory/low-stock?threshold=5
Authorization: Bearer {admin_jwt_token}
```

#### Get Items by Category

```http
GET /api/inventory/category/Diagnostic Equipment
Authorization: Bearer {admin_jwt_token}
```

### 3.4 Update Inventory

```http
PUT /api/inventory/1
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "equipmentName": "Digital Thermometer Pro",
  "description": "Non-contact infrared thermometer with memory",
  "category": "Diagnostic Equipment",
  "manufacturer": "MedTech Corp",
  "model": "MT-2024-Pro",
  "serialNumber": "MT2024001",
  "barcode": "1234567890123",
  "quantity": 30,
  "unitCost": 99.99,
  "expiryDate": "2026-12-31T00:00:00Z",
  "location": "Storage Room A",
  "notes": "Updated model with 100 reading memory"
}
```

---

## 4. InventoryAssignmentsController Testing

### 4.1 Create Assignments

#### Assign Equipment to Nurse Practitioner

```http
POST /api/inventoryassignments
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "inventoryId": 1,
  "userId": 2,
  "quantityAssigned": 2,
  "assignedDate": "2024-01-15T10:00:00Z",
  "expectedReturnDate": "2024-02-15T10:00:00Z",
  "notes": "Assignment for mobile clinic duty"
}
```

#### Assign Another Item

```http
POST /api/inventoryassignments
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "inventoryId": 2,
  "userId": 2,
  "quantityAssigned": 1,
  "assignedDate": "2024-01-15T10:00:00Z",
  "expectedReturnDate": "2024-03-15T10:00:00Z",
  "notes": "Patient monitoring assignment"
}
```

### 4.2 Retrieve Assignments

#### Get All Assignments (Admin/Provider only)

```http
GET /api/inventoryassignments
Authorization: Bearer {admin_jwt_token}
```

#### Get Paginated Assignments

```http
GET /api/inventoryassignments/paged?pageNumber=1&pageSize=10
Authorization: Bearer {admin_jwt_token}
```

#### Get Assignment by ID

```http
GET /api/inventoryassignments/1
Authorization: Bearer {admin_jwt_token}
```

#### Get Assignments by User ID

```http
GET /api/inventoryassignments/user/2
Authorization: Bearer {admin_jwt_token}
```

#### Get Current User's Assignments (as the assigned user)

```http
GET /api/inventoryassignments/my-assignments
Authorization: Bearer {nurse_jwt_token}
```

#### Get Active Assignments

```http
GET /api/inventoryassignments/active
Authorization: Bearer {admin_jwt_token}
```

#### Get Active Assignments for Specific User

```http
GET /api/inventoryassignments/active/user/2
Authorization: Bearer {admin_jwt_token}
```

#### Get Assignment History for Inventory Item

```http
GET /api/inventoryassignments/history/inventory/1
Authorization: Bearer {admin_jwt_token}
```

### 4.3 Update Assignment

```http
PUT /api/inventoryassignments/1
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "quantityAssigned": 3,
  "expectedReturnDate": "2024-02-28T10:00:00Z",
  "notes": "Extended assignment for extended clinic hours"
}
```

### 4.4 Return Assignment

```http
POST /api/inventoryassignments/return
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "assignmentId": 1,
  "quantityReturned": 2,
  "returnCondition": "Good",
  "returnNotes": "All items returned in excellent condition"
}
```

---

## 5. DashboardController Testing

### 5.1 Dashboard Statistics

```http
GET /api/dashboard/stats
Authorization: Bearer {admin_jwt_token}
```

### 5.2 Recent Items

#### Recent Inventory Items

```http
GET /api/dashboard/recent-inventories?count=5
Authorization: Bearer {admin_jwt_token}
```

#### Recent Assignments (Admin/Provider only)

```http
GET /api/dashboard/recent-assignments?count=5
Authorization: Bearer {admin_jwt_token}
```

### 5.3 Alerts

#### Expiry Alerts

```http
GET /api/dashboard/alerts/expiry
Authorization: Bearer {admin_jwt_token}
```

#### Low Stock Alerts

```http
GET /api/dashboard/alerts/low-stock
Authorization: Bearer {admin_jwt_token}
```

#### Overdue Assignment Alerts (Admin/Provider only)

```http
GET /api/dashboard/alerts/overdue
Authorization: Bearer {admin_jwt_token}
```

#### Combined Alerts Summary

```http
GET /api/dashboard/alerts/summary
Authorization: Bearer {admin_jwt_token}
```

### 5.4 Complete Dashboard Overview

```http
GET /api/dashboard/overview
Authorization: Bearer {admin_jwt_token}
```

---

## Error Testing Scenarios

### 1. Authentication Errors

#### Invalid Credentials

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "wrong@email.com",
  "password": "wrongpassword"
}
```

**Expected:** 401 Unauthorized

#### Expired Token

Use an old/expired JWT token in Authorization header.
**Expected:** 401 Unauthorized

### 2. Authorization Errors

#### Non-Admin Trying to Create User

```http
POST /api/users
Authorization: Bearer {staff_jwt_token}
Content-Type: application/json

{
  "email": "test@test.com",
  "password": "Test@123",
  "name": "Test User"
}
```

**Expected:** 403 Forbidden

### 3. Validation Errors

#### Invalid Email Format

```http
POST /api/users
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "email": "invalid-email",
  "password": "Test@123",
  "name": "Test User"
}
```

**Expected:** 400 Bad Request with validation errors

#### Duplicate Barcode

Try creating inventory with existing barcode.
**Expected:** 400 Bad Request

### 4. Business Logic Errors

#### Assign More Quantity Than Available

```http
POST /api/inventoryassignments
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "inventoryId": 1,
  "userId": 2,
  "quantityAssigned": 1000,
  "assignedDate": "2024-01-15T10:00:00Z",
  "expectedReturnDate": "2024-02-15T10:00:00Z"
}
```

**Expected:** 400 Bad Request with business rule violation

#### Assign Expired Equipment

Try assigning equipment with past expiry date.
**Expected:** 400 Bad Request

---

## Testing Tools Recommendations

### 1. **Postman Collection**

Create a Postman collection with:

- Environment variables for base URL and tokens
- Pre-request scripts for token management
- Test scripts for response validation

### 2. **PowerShell Script Testing**

```powershell
# Set base URL and token
$baseUrl = "https://localhost:7178"
$token = "your_jwt_token_here"
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test login
$loginBody = @{
    email = "admin@inventorymanagement.com"
    password = "Admin@123"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $response.data.token
```

### 3. **cURL Testing**

```bash
# Login
curl -X POST "https://localhost:7178/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@inventorymanagement.com","password":"Admin@123"}' \
  -k

# Get inventory with token
curl -X GET "https://localhost:7178/api/inventory" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -k
```

---

## Data Cleanup for Testing

### Reset Database (Development Only)

If you need to reset test data:

1. **Stop the application**
2. **Delete the database file**: `src/InventoryManagement.Infrastructure/inventory.db`
3. **Restart the application** - it will recreate the database with the admin user

### Test Data Sequences

1. **Setup Phase**: Login as admin, create users
2. **Inventory Phase**: Create inventory items with various scenarios
3. **Assignment Phase**: Create assignments, test workflows
4. **Dashboard Phase**: Verify all data appears correctly
5. **Cleanup Phase**: Return assignments, update statuses

This testing guide ensures you can thoroughly test all API endpoints while maintaining proper data flow and avoiding common errors.
