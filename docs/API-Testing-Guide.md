# API Testing Guide - Inventory Management System

## Overview

This guide provides step-by-step instructions for testing all API endpoints in the correct sequence. It reflects the current security hardening and Result pattern: unified JWT config, hashed refresh tokens, automatic model validation, auth rate limiting, idempotency, concurrency handling, accurate HTTP status codes, and `/health`.

## Base URL

- **Development**: `https://localhost:7178`
- **API Documentation**: `https://localhost:7178/openapi/v1.json`
- **Scalar API Reference**: `https://localhost:7178/scalar/v1`
- **Health Check**: `https://localhost:7178/health`

## Authentication Setup

### Step 1: Initial Admin Login

The system comes pre-seeded with an admin user for initial access.

**Local Development Admin Credentials:**

- Email: `admin@inventorymanagement.com`
- Password: `ChangeMe_LocalDev!2026`

> Production should configure `SeedData:AdminPassword`. If it is empty, a strong random password is generated and logged once at startup. There is no well-known production default password.

**Request:**

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@inventorymanagement.com",
  "password": "ChangeMe_LocalDev!2026"
}
```

**Expected Response:**

```json
{
  "isSuccess": true,
  "message": "Login successful",
  "data": {
    "accessToken": "jwt_access_token_here",
    "refreshToken": "raw_refresh_token_returned_once",
    "expiresAt": "2026-08-13T18:45:00Z",
    "user": {
      "id": 1,
      "email": "admin@inventorymanagement.com",
      "name": "System Administrator",
      "role": 1,
      "isAdmin": true,
      "isProvider": false
    }
  },
  "errors": []
}
```

Save the JWT access token and send it in the Authorization header using the bearer-token scheme. Save the refresh token; the server stores only its SHA-256 hash.

---

## Response Contract and Status Codes

Application services use `Result<T>` to express expected outcomes. Controllers map those outcomes to accurate HTTP status codes, but the JSON body remains the same `ApiResponse<T>` envelope:

```json
{
  "isSuccess": false,
  "message": "Inventory not found",
  "data": null,
  "errors": []
}
```

| Scenario | Expected status |
| -------- | --------------- |
| Successful read/update/delete | 200 OK |
| Successful create | 201 Created |
| Missing entity | 404 Not Found |
| Duplicate email/barcode/serial or concurrency conflict | 409 Conflict |
| DTO validation or generic business failure | 400 Bad Request |
| Invalid credentials/refresh token | 401 Unauthorized |
| Authenticated caller lacks a policy | 403 Forbidden |
| Auth rate limit exceeded | 429 Too Many Requests |

## Testing Flow by Controller

## 1. AuthController Testing

### 1.1 Get Current User Info

```http
GET /api/auth/me
Authorization: Bearer <accessToken>
```

### 1.2 Refresh Token

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "your_refresh_token_here"
}
```

A successful refresh returns a new access token and raw refresh token.

### 1.3 Change Password

```http
POST /api/auth/change-password
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "currentPassword": "ChangeMe_LocalDev!2026",
  "newPassword": "NewPassword@123",
  "confirmPassword": "NewPassword@123"
}
```

Changing a password revokes existing refresh tokens. For repeatable testing, either change it back or reset the development database.

### 1.4 Logout

```http
POST /api/auth/logout
Authorization: Bearer <accessToken>
```

### 1.5 Auth Rate Limit

`POST /api/auth/login` and `POST /api/auth/refresh` use fixed-window rate limiting from `RateLimiting:Auth` (default 10 requests per 60 seconds per remote IP). Excess requests return HTTP 429.

---

## 2. UsersController Testing

### 2.1 Create Users (Admin Only)

Passwords must be at least 8 characters.

#### Create a Nurse Practitioner

```http
POST /api/users
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "name": "Dr. Sarah Johnson",
  "email": "nurse1@hospital.com",
  "password": "Nurse@123",
  "role": 2,
  "isAdmin": false,
  "isProvider": true
}
```

#### Create Staff Member

```http
POST /api/users
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "name": "John Smith",
  "email": "staff1@hospital.com",
  "password": "Staff@123",
  "role": 3,
  "isAdmin": false,
  "isProvider": false
}
```

### 2.2 Retrieve and Update Users

```http
GET /api/users
Authorization: Bearer <accessToken>

GET /api/users/paged?pageNumber=1&pageSize=10
Authorization: Bearer <accessToken>

GET /api/users/1
Authorization: Bearer <accessToken>

GET /api/users/by-email/nurse1@hospital.com
Authorization: Bearer <accessToken>
```

```http
PUT /api/users/2
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "name": "Dr. Sarah Johnson-Updated",
  "email": "nurse1@hospital.com",
  "role": 2,
  "isAdmin": false,
  "isProvider": true,
  "isActive": true
}
```

Non-admin callers can update their own profile but cannot change role/admin/provider flags.

### 2.3 Role Lists

```http
GET /api/users/nurse-practitioners
Authorization: Bearer <accessToken>

GET /api/users/active
Authorization: Bearer <accessToken>
```

---

## 3. InventoryController Testing

For mutating requests outside `/api/auth`, you may send an `Idempotency-Key` header to make retries safe.

### 3.1 Create Inventory Items

#### Create Medical Equipment 1

```http
POST /api/inventory
Authorization: Bearer <accessToken>
Idempotency-Key: inv-create-thermometer-001
Content-Type: application/json

{
  "equipmentName": "Digital Thermometer",
  "description": "Non-contact infrared thermometer",
  "category": "Diagnostic Equipment",
  "brand": "MedTech Corp",
  "model": "MT-2026",
  "serialNumber": "MT2026001",
  "barcode": "1234567890123",
  "quantity": 25,
  "purchasePrice": 89.99,
  "expiryDate": "2027-12-31T00:00:00Z",
  "manufactureDate": "2026-01-15T00:00:00Z",
  "supplier": "MedSupply Inc",
  "location": "Storage Room A",
  "notes": "Temperature range: -10°C to 50°C"
}
```

#### Create Medical Equipment 2

```http
POST /api/inventory
Authorization: Bearer <accessToken>
Idempotency-Key: inv-create-bp-001
Content-Type: application/json

{
  "equipmentName": "Blood Pressure Monitor",
  "description": "Automatic digital blood pressure monitor",
  "category": "Diagnostic Equipment",
  "brand": "HealthTech Inc",
  "model": "HT-BP200",
  "serialNumber": "HT200001",
  "barcode": "2345678901234",
  "quantity": 15,
  "purchasePrice": 149.99,
  "expiryDate": "2028-06-30T00:00:00Z",
  "supplier": "HealthTech Distribution",
  "location": "Storage Room B",
  "notes": "Adult and pediatric cuffs included"
}
```

#### Create Expiring/Low Stock Item

```http
POST /api/inventory
Authorization: Bearer <accessToken>
Idempotency-Key: inv-create-syringes-001
Content-Type: application/json

{
  "equipmentName": "Disposable Syringes",
  "description": "Sterile disposable syringes 10ml",
  "category": "Disposables",
  "brand": "SafeMed",
  "model": "SM-SYR10",
  "serialNumber": "SM2026001",
  "barcode": "3456789012345",
  "quantity": 3,
  "purchasePrice": 2.50,
  "expiryDate": "2026-10-15T00:00:00Z",
  "supplier": "SafeMed Supplies",
  "location": "Storage Room C",
  "notes": "Low stock - expiring soon"
}
```

### 3.2 Retrieve, Search, and Filter

```http
GET /api/inventory
Authorization: Bearer <accessToken>

GET /api/inventory/paged?pageNumber=1&pageSize=5
Authorization: Bearer <accessToken>

GET /api/inventory/1
Authorization: Bearer <accessToken>

GET /api/inventory/barcode/1234567890123
Authorization: Bearer <accessToken>
```

```http
POST /api/inventory/search
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "searchTerm": "thermometer",
  "category": "Diagnostic Equipment",
  "pageNumber": 1,
  "pageSize": 10
}
```

```http
GET /api/inventory/available
Authorization: Bearer <accessToken>

GET /api/inventory/expiring?monthsBefore=6
Authorization: Bearer <accessToken>

GET /api/inventory/low-stock?threshold=5
Authorization: Bearer <accessToken>

GET /api/inventory/category/Diagnostic Equipment
Authorization: Bearer <accessToken>
```

### 3.3 Update Inventory

```http
PUT /api/inventory/1
Authorization: Bearer <accessToken>
Idempotency-Key: inv-update-thermometer-001
Content-Type: application/json

{
  "equipmentName": "Digital Thermometer Pro",
  "description": "Non-contact infrared thermometer with memory",
  "category": "Diagnostic Equipment",
  "brand": "MedTech Corp",
  "model": "MT-2026-Pro",
  "serialNumber": "MT2026001",
  "barcode": "1234567890123",
  "quantity": 30,
  "purchasePrice": 99.99,
  "expiryDate": "2027-12-31T00:00:00Z",
  "manufactureDate": "2026-01-15T00:00:00Z",
  "supplier": "MedSupply Inc",
  "status": 1,
  "location": "Storage Room A",
  "notes": "Updated model with 100 reading memory"
}
```

---

## 4. InventoryAssignmentsController Testing

Assignment create/return operations run in a transaction and use optimistic concurrency. Concurrent conflicting stock updates return HTTP 409.

### 4.1 Create Assignments

```http
POST /api/inventoryassignments
Authorization: Bearer <accessToken>
Idempotency-Key: assign-thermometer-user2-001
Content-Type: application/json

{
  "inventoryId": 1,
  "userId": 2,
  "assignedQuantity": 2,
  "expectedReturnDate": "2026-09-15T10:00:00Z",
  "assignmentNotes": "Assignment for mobile clinic duty"
}
```

```http
POST /api/inventoryassignments
Authorization: Bearer <accessToken>
Idempotency-Key: assign-bp-user2-001
Content-Type: application/json

{
  "inventoryId": 2,
  "userId": 2,
  "assignedQuantity": 1,
  "expectedReturnDate": "2026-10-15T10:00:00Z",
  "assignmentNotes": "Patient monitoring assignment"
}
```

### 4.2 Retrieve Assignments

```http
GET /api/inventoryassignments
Authorization: Bearer <accessToken>

GET /api/inventoryassignments/paged?pageNumber=1&pageSize=10
Authorization: Bearer <accessToken>

GET /api/inventoryassignments/1
Authorization: Bearer <accessToken>

GET /api/inventoryassignments/user/2
Authorization: Bearer <accessToken>

GET /api/inventoryassignments/my-assignments
Authorization: Bearer <accessToken>

GET /api/inventoryassignments/active
Authorization: Bearer <accessToken>

GET /api/inventoryassignments/active/user/2
Authorization: Bearer <accessToken>

GET /api/inventoryassignments/history/inventory/1
Authorization: Bearer <accessToken>
```

### 4.3 Update Assignment

```http
PUT /api/inventoryassignments/1
Authorization: Bearer <accessToken>
Idempotency-Key: assignment-update-001
Content-Type: application/json

{
  "assignedQuantity": 3,
  "expectedReturnDate": "2026-09-28T10:00:00Z",
  "status": 1,
  "assignmentNotes": "Extended assignment for extended clinic hours"
}
```

### 4.4 Return Assignment

```http
POST /api/inventoryassignments/return
Authorization: Bearer <accessToken>
Idempotency-Key: assignment-return-001
Content-Type: application/json

{
  "assignmentId": 1,
  "returnNotes": "All items returned in excellent condition"
}
```

---

## 5. DashboardController Testing

Dashboard aggregate endpoints execute queries sequentially to avoid shared DbContext concurrency errors.

```http
GET /api/dashboard/stats
Authorization: Bearer <accessToken>

GET /api/dashboard/recent-inventories?count=5
Authorization: Bearer <accessToken>

GET /api/dashboard/recent-assignments?count=5
Authorization: Bearer <accessToken>

GET /api/dashboard/alerts/expiry
Authorization: Bearer <accessToken>

GET /api/dashboard/alerts/low-stock
Authorization: Bearer <accessToken>

GET /api/dashboard/alerts/overdue
Authorization: Bearer <accessToken>

GET /api/dashboard/alerts/summary
Authorization: Bearer <accessToken>

GET /api/dashboard/overview
Authorization: Bearer <accessToken>
```

---

## 6. Health Check Testing

```http
GET /health
```

**Expected:** 200 OK when the application is healthy.

---

## Idempotency Testing

### Replay Same Request

Send the same mutating request twice with the same `Idempotency-Key` and identical body.

**Expected:** The second response replays the first result and includes:

```http
Idempotency-Replayed: true
```

### In-Progress Conflict

Send two concurrent mutating requests with the same key.

**Expected:** One proceeds; the other returns 409 Conflict.

### Key Mismatch

Reuse an `Idempotency-Key` with a different body, method, or path before retention expires.

**Expected:** 422 Unprocessable Entity with a key mismatch message.

### Auth Exclusion

Send `Idempotency-Key` to `/api/auth/login`.

**Expected:** Auth endpoints are excluded; token responses are not cached/replayed.

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

**Expected:** 401 Unauthorized with a uniform invalid credentials response.

#### Expired Token

Use an old/expired JWT token in the Authorization header.

**Expected:** 401 Unauthorized.

#### Auth Rate Limit

Send more than the configured number of login attempts within the fixed window.

**Expected:** 429 Too Many Requests.

### 2. Authorization Errors

#### Non-Admin Trying to Create User

```http
POST /api/users
Authorization: Bearer <nonAdminAccessToken>
Content-Type: application/json

{
  "email": "test@test.com",
  "password": "Test@123",
  "name": "Test User",
  "role": 3,
  "isAdmin": false,
  "isProvider": false
}
```

**Expected:** 403 Forbidden.

### 3. Validation Errors

#### Invalid Email Format

```http
POST /api/users
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "email": "invalid-email",
  "password": "Test@123",
  "name": "Test User",
  "role": 3
}
```

**Expected:** 400 Bad Request with `ApiResponse` message `Validation failed` and an errors list.

#### Password Too Short

```http
POST /api/users
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "email": "short@test.com",
  "password": "short",
  "name": "Short Password User",
  "role": 3
}
```

**Expected:** 400 Bad Request. Password policy minimum is 8 characters.

### 4. Business Logic Errors

#### Assign More Quantity Than Available

```http
POST /api/inventoryassignments
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "inventoryId": 1,
  "userId": 2,
  "assignedQuantity": 1000,
  "expectedReturnDate": "2026-09-15T10:00:00Z"
}
```

**Expected:** 400 Bad Request with the `ApiResponse` error message from `Result.Failure`.

#### Concurrent Assignment Conflict

Submit two simultaneous assignment requests that consume the same remaining stock.

**Expected:** One succeeds; the conflicting update returns HTTP 409.

---

## Testing Tools Recommendations

### 1. Postman Collection

Create a Postman collection with environment variables for base URL, access token, refresh token, and idempotency keys.

### 2. PowerShell Script Testing

```powershell
$baseUrl = "https://localhost:7178"
$loginBody = @{
    email = "admin@inventorymanagement.com"
    password = "ChangeMe_LocalDev!2026"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $response.data.accessToken
$refreshToken = $response.data.refreshToken

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Invoke-RestMethod -Uri "$baseUrl/health" -Method GET
```

### 3. cURL Testing

```bash
curl -X POST "https://localhost:7178/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@inventorymanagement.com","password":"ChangeMe_LocalDev!2026"}' \
  -k

curl -X GET "https://localhost:7178/api/inventory" \
  -H "Authorization: Bearer <accessToken>" \
  -k

curl -X GET "https://localhost:7178/health" -k
```

---

## Data Cleanup for Testing

### Reset Database (Development Only)

1. **Stop the application**.
2. **Delete the database file**: `server\InventoryManagement.Infrastructure\inventory.db`.
3. **Restart the application** - it will migrate the database and seed the admin user.

### Test Data Sequences

1. **Setup Phase**: Login as admin, create users.
2. **Inventory Phase**: Create inventory items with various scenarios.
3. **Assignment Phase**: Create assignments, test workflows, idempotency, and concurrency.
4. **Dashboard Phase**: Verify all data appears correctly.
5. **Cleanup Phase**: Return assignments, logout, or reset the database.

This testing guide ensures you can thoroughly test all API endpoints while maintaining proper data flow and avoiding common errors.

---

## Group 2 Endpoints (stock, suppliers, locations, maintenance)

All require a Bearer token. **Reads** are available to all roles; **writes** require Admin or Provider; **deletes** require Admin. Send an `Idempotency-Key` header on POSTs for retry safety.

### Stock operations (InventoryController)

| Method | Route | Body | Notes |
| --- | --- | --- | --- |
| POST | `/api/inventory/{id}/receive` | `{ "quantity": 40, "unitCost": 2.5, "supplierId": 1, "reason": "PO#1001" }` | Qty↑ & Available↑; appends `Received` |
| POST | `/api/inventory/{id}/adjust` | `{ "quantityDelta": -3, "reason": "Breakage" }` | reason required; reject if available < 0 |
| POST | `/api/inventory/{id}/dispose` | `{ "quantity": 2, "reason": "Expired" }` | ≤ available; qty→0 sets `Disposed` |
| POST | `/api/inventory/{id}/transfer` | `{ "toLocationId": 2, "reason": "Rebalance" }` | appends `Transferred` |
| GET | `/api/inventory/{id}/movements` | — | item ledger, newest first |
| GET | `/api/inventory/movements/recent?pageNumber=1&pageSize=20` | — | global recent (Admin/Provider) |
| GET | `/api/inventory/reorder` | — | items where available ≤ reorder level |

### Suppliers (`/api/suppliers`) and Locations (`/api/locations`)

Standard CRUD: `GET /` (`?activeOnly=true`), `GET /paged`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`.

```jsonc
// POST /api/suppliers
{ "name": "Acme Medical", "contactName": "Jane", "email": "sales@acme.test", "leadTimeDays": 7 }
// POST /api/locations
{ "name": "Central Store", "code": "CS-01", "parentLocationId": null }
```
Delete guards: a supplier with linked items → **409**; a location with children or linked items → **409**.

### Assignment lifecycle

| Method | Route | Body |
| --- | --- | --- |
| POST | `/api/inventoryassignments/return` | `{ "assignmentId": 3, "returnQuantity": 2, "returnCondition": 1, "returnNotes": "…" }` (partial + condition) |
| POST | `/api/inventoryassignments/renew` | `{ "assignmentId": 3, "newExpectedReturnDate": "2026-09-30T00:00:00Z", "notes": "…" }` |
| GET | `/api/inventoryassignments/due-soon?daysAhead=7` | — |

`returnQuantity` omitted returns the full outstanding amount. `returnCondition`: 1 Good, 2 Damaged, 3 Lost, 4 NeedsRepair (Lost reduces owned quantity instead of crediting stock).

### Maintenance (`/api/maintenance`)

| Method | Route | Body |
| --- | --- | --- |
| GET | `/api/maintenance/paged` · `/due?daysAhead=30` · `/inventory/{inventoryId}` · `/{id}` | — |
| POST | `/api/maintenance` | `{ "inventoryId": 7, "maintenanceType": 2, "title": "Annual calibration", "intervalDays": 365, "nextDueAt": "2026-09-10T00:00:00Z" }` |
| PUT | `/api/maintenance/{id}` | full update incl. `status` |
| POST | `/api/maintenance/{id}/complete` | `{ "performedAt": "2026-08-21T00:00:00Z", "notes": "OK" }` — recurring rolls `nextDueAt` forward |
| DELETE | `/api/maintenance/{id}` | — (Admin) |

`maintenanceType`: 1 Inspection, 2 Calibration, 3 Service, 4 Repair, 5 Cleaning. Status is normalized from the due date on read (Scheduled/Due/Overdue) for open schedules.
