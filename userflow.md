# User Flow Guide (SuperAdmin, Agency, Applicant)

This document explains how each user role operates in the system, with focus on real actions and API/UI path.

## 1) Role Model and Tenant Concept

- A **tenant** is represented by `TenantId` on user and business entities.
  - `SharedKernel/Models/BaseEntity.cs:6`
  - `SharedKernel/Models/User.cs:2`
- Public self-registration creates only `Applicant` users.
  - `LicenseService/Controllers/AuthController.cs:39`
- `Admin` can create `Agency`, `Admin`, and `Applicant` users in any tenant using `POST /api/Auth/users`.
  - `LicenseService/Controllers/AuthController.cs:98`

Important product behavior:
- There is no separate `Tenant` table yet.
- “Create new tenant” is done by creating users with a new `TenantId` value (for example `tenant2`).

---

## 2) HTTPS and Access URLs

## 2.1 Start HTTPS-enabled Docker stack

1. Generate a local cert:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\scripts\generate-https-cert.ps1
   ```
2. Start stack with HTTPS override:
   ```powershell
   docker compose -f docker-compose.yml -f docker-compose.https.yml up --build
   ```

## 2.2 URLs

- Frontend HTTPS: `https://localhost:5444`
- Frontend HTTP (fallback): `http://localhost:5005`
- Gateway HTTPS: `https://localhost:5443`
- Gateway HTTP: `http://localhost:5000`

Config references:
- `docker-compose.https.yml:15`
- `docker-compose.https.yml:4`

---

## 3) SuperAdmin Flow (Bootstrap + Manage Tenants)

## 3.1 Default bootstrap admin (Docker)

The compose file seeds a dev admin user:
- Username: `admin`
- Password: `Admin123!`
- Tenant: `tenant1`

Reference:
- `docker-compose.yml:19`
- `LicenseService/AdminSeeder.cs:20`

## 3.2 SuperAdmin login

1. Open frontend login page: `https://localhost:5444/Auth/Login`.
2. Enter admin credentials and `tenant1`.
3. On success, JWT cookie is issued and role claim contains `Admin`.

Code path:
- UI submit: `LicenseFrontend/Views/Auth/Login.cshtml:24`
- Controller login call: `LicenseFrontend/Controllers/AuthController.cs:33`
- Token creation: `LicenseService/Controllers/AuthController.cs:81`

## 3.3 Create a new tenant

Because tenant is claim/data driven, “creating tenant” means creating first user with a new `TenantId`.

### API call (through gateway)

`POST /license/api/Auth/users`  
Auth: Bearer token of admin user  
Body example:
```json
{
  "username": "agency_admin_t2",
  "password": "StrongPass#2026",
  "tenantId": "tenant2",
  "role": "Agency",
  "agency": "Medical Licensing Board"
}
```

What happens:
1. Endpoint validates caller is `Admin`.
2. Validates role and agency rules.
3. Stores user in `Users` table with BCrypt password hash.
4. That `tenantId` now behaves as an active tenant partition.

Code path:
- Authorization + validation: `LicenseService/Controllers/AuthController.cs:98`
- Password hashing: `LicenseService/Controllers/AuthController.cs:118`

## 3.4 Create more users inside a tenant

Examples:
- Create `Applicant` in `tenant2`
- Create second `Agency` reviewer in `tenant2`
- Create another admin in `tenant2`

All via the same endpoint: `POST /license/api/Auth/users`.

---

## 4) Applicant User Flow

## 4.1 Register

1. Open `https://localhost:5444/Auth/Register`.
2. Fill username/password/tenantId.
3. Registration creates **Applicant** only.

Code path:
- Form: `LicenseFrontend/Views/Auth/Register.cshtml:20`
- Backend enforcement: `LicenseService/Controllers/AuthController.cs:39`

## 4.2 Login

1. Login through `/Auth/Login`.
2. JWT cookie (`jwt`) is set.
3. Redirect goes to Applicant dashboard.

Code path:
- Cookie set: `LicenseFrontend/Controllers/AuthController.cs:44`
- Redirect decision: `LicenseFrontend/Controllers/AuthController.cs:52`

## 4.3 Submit license application

From Applicant dashboard:
1. Fill agency + upload document.
2. Submit form.

Orchestration inside MVC controller:
1. Upload document API call.
2. Create license API call.
3. Process payment API call.
4. Create notification API call.

Code path:
- Form submit: `LicenseFrontend/Views/Dashboard/Applicant.cshtml:95`
- Orchestration: `LicenseFrontend/Controllers/LicensesController.cs:40`

Backend behavior:
- Document validated and stored: `DocumentService/Controllers/DocumentsController.cs:26`
- License created with generated number: `LicenseService/Handlers/ApplyLicenseHandler.cs:25`
- Payment ownership validated: `PaymentService/Controllers/PaymentsController.cs:30`

## 4.4 Track status and notifications

- Applicant dashboard fetches `/license/api/Licenses` and `/notification/api/Notifications`.
- Only applicant’s own records are shown due role filters and tenant filters.

Code path:
- Dashboard API calls: `LicenseFrontend/Controllers/DashboardController.cs:26`
- Query filtering: `LicenseService/Handlers/GetLicensesHandler.cs:17`

---

## 5) Agency User Flow

## 5.1 Provisioning by admin

Agency users are created only by admin via `POST /license/api/Auth/users` with:
- `role = "Agency"`
- non-empty `agency` field

Validation:
- `LicenseService/Controllers/AuthController.cs:109`

## 5.2 Login and review queue

1. Agency logs in with tenant credentials.
2. Agency dashboard loads pending applications for its agency board.

Filtering:
- Query includes `Agency` claim and applies agency predicate.
- `LicenseService/Handlers/GetLicensesHandler.cs:21`

## 5.3 Approve/Reject

1. Agency clicks Approve or Reject in dashboard table.
2. MVC sends `PUT /license/api/Licenses/{id}/status`.
3. LicenseService verifies caller role + agency ownership.

Code path:
- Dashboard action forms: `LicenseFrontend/Views/Dashboard/Agency.cshtml:85`
- API endpoint auth: `LicenseService/Controllers/LicensesController.cs:40`
- Agency ownership guard: `LicenseService/Handlers/UpdateLicenseStatusHandler.cs:25`

Result:
- License status transitions from Pending -> Approved/Rejected.
- Applicant notification is inserted.
  - `LicenseService/Handlers/UpdateLicenseStatusHandler.cs:37`

---

## 6) Renewal Background Flow

1. Hangfire daily job runs (`renewal-job`).
2. Reads all tenant IDs from users table.
3. For each tenant: finds approved licenses expiring within 7 days.
4. Extends expiry by one year.
5. Inserts notification for each affected user.

Code path:
- Job registration: `LicenseService/Program.cs:132`
- Tenant iteration + renewal logic: `LicenseService/RenewalJob.cs:20`

---

## 7) API and Gateway User Path

Frontend does not call internal service ports directly. It calls gateway routes:
- `/license/*`
- `/document/*`
- `/payment/*`
- `/notification/*`

Gateway routing definitions:
- Docker: `ApiGateway/ocelot.docker.json:2`
- Local: `ApiGateway/ocelot.json:2`

Example:
- Frontend login call: `LicenseFrontend/Controllers/AuthController.cs:33`

---

## 8) Troubleshooting Checklist for User Flows

## 8.1 Cannot login
- Verify tenantId and user role exist in same tenant.
- Verify JWT key is consistent across all services + gateway.
  - `docker-compose.yml:16`

## 8.2 Agency sees no applications
- Check applicant selected matching agency board in form.
  - `LicenseFrontend/Views/Dashboard/Applicant.cshtml:103`
- Check agency user has correct `Agency` claim value.
  - `LicenseService/Controllers/AuthController.cs:73`

## 8.3 Document download fails
- Ensure requested document belongs to same tenant.
- Ensure file still exists in uploads volume.
  - `DocumentService/Controllers/DocumentsController.cs:64`

## 8.4 Tenant isolation validation
- Login as tenant1 user and tenant2 user; verify each sees only own tenant records.
- Isolation is enforced by EF filter:
  - `SharedKernel/Data/LicenseDbContext.cs:20`

