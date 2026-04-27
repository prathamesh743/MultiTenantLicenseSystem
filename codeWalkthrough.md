# Multi-Tenant License System: Detailed Code Walkthrough

## 1) Requirement-to-Implementation Map

This section maps each original requirement to what we implemented and where it lives.

| Requirement | Implementation | Key Files |
| --- | --- | --- |
| ASP.NET MVC frontend with role-based dashboards | ASP.NET Core MVC app with JWT cookie auth, role policies, anti-forgery, applicant + agency/admin dashboards | `LicenseFrontend/Program.cs:13`, `LicenseFrontend/Controllers/AuthController.cs:26`, `LicenseFrontend/Controllers/DashboardController.cs:19`, `LicenseFrontend/Views/Dashboard/Applicant.cshtml:95`, `LicenseFrontend/Views/Dashboard/Agency.cshtml:85` |
| 3-4 microservices | `LicenseService`, `DocumentService`, `PaymentService`, `NotificationService` as independent ASP.NET Core Web APIs | `LicenseService/Program.cs:12`, `DocumentService/Program.cs:9`, `PaymentService/Program.cs:9`, `NotificationService/Program.cs:9` |
| SQL Server multi-tenancy isolation | Global query filters by `TenantId`, per-request tenant injection from JWT claim | `SharedKernel/Data/LicenseDbContext.cs:20`, `SharedKernel/Tenancy/HttpContextTenantProvider.cs:15`, `LicenseService/Program.cs:25` |
| REST APIs with JWT auth/authorization | JWT validation in all services + role-based endpoint protection | `LicenseService/Program.cs:56`, `ApiGateway/Program.cs:23`, `LicenseService/Controllers/LicensesController.cs:40`, `LicenseService/Controllers/AuthController.cs:98` |
| CQRS pattern for license workflow | MediatR commands/queries/handlers for apply/get/update flows | `LicenseService/Commands/ApplyLicenseCommand.cs:7`, `LicenseService/Queries/GetLicensesQuery.cs:5`, `LicenseService/Handlers/ApplyLicenseHandler.cs:12`, `LicenseService/Handlers/GetLicensesHandler.cs:9`, `LicenseService/Handlers/UpdateLicenseStatusHandler.cs:10` |
| Background job processing for renewals | Hangfire recurring job scans tenants and renews expiring approved licenses | `LicenseService/Program.cs:132`, `LicenseService/RenewalJob.cs:16` |
| API Gateway integration | Ocelot routes gateway -> downstream services; frontend talks via gateway URL | `ApiGateway/ocelot.docker.json:2`, `ApiGateway/Program.cs:48`, `LicenseFrontend/Controllers/AuthController.cs:33`, `LicenseFrontend/Controllers/LicensesController.cs:76` |
| Docker-ready deployment | Compose stack with SQL Server, all services, seed admin, migrations, health checks | `docker-compose.yml:3`, `LicenseService/Program.cs:116`, `DocumentService/Program.cs:84`, `PaymentService/Program.cs:85`, `NotificationService/Program.cs:84` |
| HTTPS in Docker | Optional HTTPS override compose + cert generation script | `docker-compose.https.yml:3`, `scripts/generate-https-cert.ps1:1` |

---

## 2) Build-Order Narrative (How We Built It)

This is the sequence of technical actions in practical implementation order.

### Step 1: Create domain + persistence base
1. We created domain entities inheriting a base with `Id` and `TenantId` so every business row is tenant-tagged by design.  
   - `SharedKernel/Models/BaseEntity.cs:3`
2. We added concrete models for licensing workflow (`License`, `Document`, `PaymentRecord`, `Notification`, `User`).  
   - `SharedKernel/Models/License.cs:2`, `SharedKernel/Models/User.cs:2`
3. We built `LicenseDbContext` with EF Core global query filters so reads are auto-isolated by tenant.  
   - `SharedKernel/Data/LicenseDbContext.cs:20`

Why this matters for requirement context: multi-tenancy isolation is enforced at ORM query level, not left to manual `WHERE` clauses.

### Step 2: Inject tenant context at runtime
1. We introduced `ITenantProvider` abstraction to resolve current tenant from JWT claim.  
   - `SharedKernel/Tenancy/ITenantProvider.cs:3`
2. We implemented `HttpContextTenantProvider` to read claim `TenantId`.  
   - `SharedKernel/Tenancy/HttpContextTenantProvider.cs:15`
3. In each service `Program.cs`, we build `LicenseDbContext` per request and set `ctx.TenantId`.  
   - `LicenseService/Program.cs:25`, `DocumentService/Program.cs:18`, `NotificationService/Program.cs:18`, `PaymentService/Program.cs:18`

Why this matters: each HTTP request gets a tenant-scoped EF context automatically.

### Step 3: Establish secure identity/auth
1. We centralized JWT config shape (`Issuer`, `Audience`, `Key`) in `JwtOptions`.  
   - `SharedKernel/Security/JwtOptions.cs:3`
2. We bind options from `appsettings`/environment and fail startup if key missing.  
   - `LicenseService/Program.cs:49`, `ApiGateway/Program.cs:16`, `LicenseFrontend/Program.cs:6`
3. We hardened password handling to BCrypt hash + verify (no plaintext password storage).  
   - `LicenseService/Controllers/AuthController.cs:38`, `LicenseService/Controllers/AuthController.cs:62`
4. Public registration is restricted to `Applicant`; only admin can create `Agency`/`Admin`.  
   - `LicenseService/Controllers/AuthController.cs:39`, `LicenseService/Controllers/AuthController.cs:98`

Why this matters: principle of least privilege + secure credential storage.

### Step 4: Implement CQRS and workflow APIs
1. Command: apply license (`ApplyLicenseCommand`) and handler persists `License` with generated number.  
   - `LicenseService/Commands/ApplyLicenseCommand.cs:7`, `LicenseService/Handlers/ApplyLicenseHandler.cs:21`
2. Query: fetch licenses with role-aware filters (Applicant by `UserId`, Agency by `Agency`).  
   - `LicenseService/Handlers/GetLicensesHandler.cs:17`
3. Command: update status guarded by role and agency ownership, plus notification creation.  
   - `LicenseService/Controllers/LicensesController.cs:40`, `LicenseService/Handlers/UpdateLicenseStatusHandler.cs:25`

Why this matters: CQRS keeps write intent and read intent explicit and testable.

### Step 5: Add supporting microservices
1. Document upload/download with file-type/size validation, randomized stored filename, tenant-safe query.  
   - `DocumentService/Controllers/DocumentsController.cs:27`, `DocumentService/Controllers/DocumentsController.cs:37`, `DocumentService/Controllers/DocumentsController.cs:61`
2. Payment service validates amount and license ownership before writing payment record.  
   - `PaymentService/Controllers/PaymentsController.cs:28`, `PaymentService/Controllers/PaymentsController.cs:30`
3. Notification service returns current-user notifications and allows controlled creation.  
   - `NotificationService/Controllers/NotificationsController.cs:22`, `NotificationService/Controllers/NotificationsController.cs:47`

Why this matters: each bounded context owns its own API concerns.

### Step 6: Add background renewal processing
1. Registered Hangfire SQL storage/server.  
   - `LicenseService/Program.cs:44`
2. Added recurring daily renewal job registration.  
   - `LicenseService/Program.cs:132`
3. `RenewalJob` iterates tenants, renews expiring approved licenses, writes notifications.  
   - `LicenseService/RenewalJob.cs:20`, `LicenseService/RenewalJob.cs:33`, `LicenseService/RenewalJob.cs:45`
4. Hangfire dashboard locked to authenticated admin users only.  
   - `LicenseService/Program.cs:111`, `LicenseService/HangfireDashboardAdminOnlyAuthorizationFilter.cs:5`

Why this matters: requirement explicitly asked for renewal background processing.

### Step 7: Wire API Gateway and frontend orchestration
1. Ocelot routes all service calls behind a single gateway entrypoint.  
   - `ApiGateway/ocelot.docker.json:2`
2. Gateway validates JWT before proxying protected routes.  
   - `ApiGateway/Program.cs:23`
3. Frontend uses configurable `GatewayUrl` and no longer depends on direct service ports.  
   - `LicenseFrontend/Controllers/AuthController.cs:17`, `LicenseFrontend/Controllers/LicensesController.cs:76`

Why this matters: decouples UI from internal topology and supports deployment portability.

### Step 8: Production hardening defaults
1. Auto migration hooks with environment-controlled toggle.  
   - `LicenseService/Program.cs:119`, `DocumentService/Program.cs:87`, `NotificationService/Program.cs:87`, `PaymentService/Program.cs:88`
2. Health endpoints exposed in each service and gateway/frontend.  
   - `LicenseService/Program.cs:138`, `ApiGateway/Program.cs:46`, `LicenseFrontend/Program.cs:58`
3. Anti-forgery added to MVC forms and actions.  
   - `LicenseFrontend/Controllers/AuthController.cs:27`, `LicenseFrontend/Controllers/LicensesController.cs:32`, `LicenseFrontend/Views/Auth/Login.cshtml:25`
4. Docker seeding for admin bootstrap in non-prod-friendly way.  
   - `LicenseService/AdminSeeder.cs:20`, `docker-compose.yml:19`

---

## 3) Core Design Principles and Patterns Used

## 3.1 SOLID and OOP
- **Single Responsibility**: each service handles one domain area; controllers orchestrate HTTP only, handlers execute business logic.  
  - `LicenseService/Handlers/ApplyLicenseHandler.cs:12`, `PaymentService/Controllers/PaymentsController.cs:13`
- **Dependency Inversion**: tenant resolution and cross-tenant context creation via interfaces (`ITenantProvider`, `ILicenseDbContextTenantFactory`).  
  - `SharedKernel/Tenancy/ITenantProvider.cs:3`, `SharedKernel/Data/ILicenseDbContextTenantFactory.cs:3`
- **Encapsulation**: domain entities hold state with default invariants (`Status = "Pending"`).  
  - `SharedKernel/Models/License.cs:11`

## 3.2 Security Best Practices
- Password hashing using BCrypt.  
  - `LicenseService/Controllers/AuthController.cs:38`
- JWT secret not hardcoded in code; loaded from configuration and required at startup.  
  - `LicenseService/Program.cs:51`, `ApiGateway/Program.cs:18`
- Role-based authorization for sensitive operations (`CreateUser`, license approve/reject).  
  - `LicenseService/Controllers/AuthController.cs:98`, `LicenseService/Controllers/LicensesController.cs:40`
- CSRF mitigation in MVC forms.  
  - `LicenseFrontend/Views/Dashboard/Agency.cshtml:86`, `LicenseFrontend/Controllers/LicensesController.cs:112`

## 3.3 Multi-Tenancy Isolation Strategy
- Shared database + discriminator (`TenantId`) model.
- Automatic filter at DbContext model level.  
  - `SharedKernel/Data/LicenseDbContext.cs:20`
- Tenant from claim injected into DbContext per request.  
  - `LicenseService/Program.cs:29`
- Explicit controlled bypass only in system-level scenarios (seeding, renewal).  
  - `LicenseService/AdminSeeder.cs:35`, `LicenseService/RenewalJob.cs:21`

## 3.4 API and Integration Patterns
- API Gateway pattern (Ocelot) for edge routing and central auth.  
  - `ApiGateway/ocelot.docker.json:2`
- Backend-for-Frontend style orchestration in MVC controller for multi-step workflow.  
  - `LicenseFrontend/Controllers/LicensesController.cs:40`
- Command/Query segregation with MediatR.

---

## 4) Microservice-by-Microservice Technical Breakdown

## 4.1 LicenseService

### `Program.cs`
- Registers DB, auth, Swagger, MediatR, Hangfire, health checks.  
  - `LicenseService/Program.cs:19`, `LicenseService/Program.cs:56`, `LicenseService/Program.cs:41`, `LicenseService/Program.cs:44`, `LicenseService/Program.cs:72`
- Applies migrations + seeds admin user at startup.  
  - `LicenseService/Program.cs:116`, `LicenseService/Program.cs:126`
- Registers recurring renewal job.  
  - `LicenseService/Program.cs:132`

### `AuthController.cs`
- `POST /api/Auth/register`: creates Applicant only, hashes password.  
  - `LicenseService/Controllers/AuthController.cs:26`
- `POST /api/Auth/login`: validates password hash and issues JWT with role + tenant + agency claims.  
  - `LicenseService/Controllers/AuthController.cs:50`
- `POST /api/Auth/users`: admin-only user provisioning for Applicant/Agency/Admin (including tenant).  
  - `LicenseService/Controllers/AuthController.cs:98`

### `LicensesController.cs`
- `POST /api/Licenses`: applicant submission.
- `GET /api/Licenses`: role-aware listing.
- `PUT /api/Licenses/{id}/status`: only Agency/Admin and status constrained to Approved/Rejected.  
  - `LicenseService/Controllers/LicensesController.cs:17`, `LicenseService/Controllers/LicensesController.cs:27`, `LicenseService/Controllers/LicensesController.cs:39`

### CQRS artifacts
- Commands/Queries model request intent.  
  - `LicenseService/Commands/ApplyLicenseCommand.cs:7`, `LicenseService/Commands/UpdateLicenseStatusCommand.cs:6`, `LicenseService/Queries/GetLicensesQuery.cs:5`
- Handlers execute the real logic and DB writes.  
  - `LicenseService/Handlers/ApplyLicenseHandler.cs:17`, `LicenseService/Handlers/GetLicensesHandler.cs:13`, `LicenseService/Handlers/UpdateLicenseStatusHandler.cs:19`

### Background processing and admin tooling
- `RenewalJob`: per-tenant renewal + notifications.  
  - `LicenseService/RenewalJob.cs:16`
- `AdminSeeder`: startup bootstrap of admin account from config.  
  - `LicenseService/AdminSeeder.cs:9`
- `HangfireDashboardAdminOnlyAuthorizationFilter`: protects dashboard from non-admins.  
  - `LicenseService/HangfireDashboardAdminOnlyAuthorizationFilter.cs:3`

## 4.2 DocumentService
- Startup setup mirrors auth + tenant-scoped DbContext pattern.  
  - `DocumentService/Program.cs:16`, `DocumentService/Program.cs:37`
- `DocumentsController` performs upload validation and tenant-safe download.  
  - `DocumentService/Controllers/DocumentsController.cs:23`, `DocumentService/Controllers/DocumentsController.cs:58`

## 4.3 PaymentService
- Startup setup same security and DB pattern.  
  - `PaymentService/Program.cs:31`
- `PaymentsController` validates amount and ensures payer owns the license.  
  - `PaymentService/Controllers/PaymentsController.cs:28`, `PaymentService/Controllers/PaymentsController.cs:32`

## 4.4 NotificationService
- Startup setup same security and DB pattern.  
  - `NotificationService/Program.cs:31`
- `NotificationsController` returns only current user’s notifications and safely controls notification creation target.  
  - `NotificationService/Controllers/NotificationsController.cs:22`, `NotificationService/Controllers/NotificationsController.cs:47`

## 4.5 ApiGateway
- Reads Ocelot config file from settings for local/docker switching.  
  - `ApiGateway/Program.cs:10`
- Validates JWT at edge and runs Ocelot middleware.  
  - `ApiGateway/Program.cs:23`, `ApiGateway/Program.cs:48`
- Local routes in `ocelot.json`, container routes in `ocelot.docker.json`.  
  - `ApiGateway/ocelot.json:2`, `ApiGateway/ocelot.docker.json:2`

## 4.6 LicenseFrontend (MVC UI)

### Startup and security wiring
- JWT auth from cookie + role policies + anti-forgery + health endpoint.  
  - `LicenseFrontend/Program.cs:13`, `LicenseFrontend/Program.cs:44`, `LicenseFrontend/Program.cs:50`

### Controllers
- `AuthController`: login/register/logout against gateway API and secure cookie settings.  
  - `LicenseFrontend/Controllers/AuthController.cs:26`, `LicenseFrontend/Controllers/AuthController.cs:44`
- `DashboardController`: role-specific pages and fetches from gateway.  
  - `LicenseFrontend/Controllers/DashboardController.cs:19`, `LicenseFrontend/Controllers/DashboardController.cs:46`
- `LicensesController`: orchestrates document upload -> license create -> payment -> notification.  
  - `LicenseFrontend/Controllers/LicensesController.cs:40`, `LicenseFrontend/Controllers/LicensesController.cs:71`, `LicenseFrontend/Controllers/LicensesController.cs:90`, `LicenseFrontend/Controllers/LicensesController.cs:97`

### Models
- UI DTOs map backend JSON response shape for display.
  - `LicenseFrontend/Models/LicenseViewModel.cs:7`
  - `LicenseFrontend/Models/NotificationViewModel.cs:7`

### Views
- Login/Register forms include anti-forgery tokens.  
  - `LicenseFrontend/Views/Auth/Login.cshtml:24`, `LicenseFrontend/Views/Auth/Register.cshtml:20`
- Applicant and Agency dashboards provide role-specific actions.  
  - `LicenseFrontend/Views/Dashboard/Applicant.cshtml:95`, `LicenseFrontend/Views/Dashboard/Agency.cshtml:85`

---

## 5) Configuration and Deployment Walkthrough

### Why we changed `appsettings`
- We moved JWT and startup behavior into config so code is environment-agnostic.
- Example in LicenseService:
  - JWT section added: `LicenseService/appsettings.json:6`
  - Auto migration toggle: `LicenseService/appsettings.json:11`
  - Seed section: `LicenseService/appsettings.json:14`

### Docker build/runtime setup
- Base stack (`docker-compose.yml`) runs services over HTTP internally for inter-service communication and boots SQL + admin seed.  
  - `docker-compose.yml:3`
- Optional HTTPS edge stack (`docker-compose.https.yml`) exposes TLS ports for frontend/gateway.  
  - `docker-compose.https.yml:3`
- Cert generation helper script creates `certs/localhost.pfx` using `dotnet dev-certs`.  
  - `scripts/generate-https-cert.ps1:1`

---

## 6) End-to-End Request Example (Applicant Apply Flow)

1. User logs in via MVC (`/Auth/Login`) and receives JWT in secure cookie.
   - `LicenseFrontend/Controllers/AuthController.cs:44`
2. Applicant submits form on dashboard; MVC controller uploads file to document service through gateway.
   - `LicenseFrontend/Controllers/LicensesController.cs:52`
3. MVC controller creates license in LicenseService.
   - `LicenseFrontend/Controllers/LicensesController.cs:76`
4. MVC controller posts payment in PaymentService.
   - `LicenseFrontend/Controllers/LicensesController.cs:94`
5. MVC controller posts user notification in NotificationService.
   - `LicenseFrontend/Controllers/LicensesController.cs:102`
6. Agency/Admin later approves/rejects license via protected endpoint.
   - `LicenseService/Controllers/LicensesController.cs:40`

---

## 7) Notes for Beginners (How to Read This Solution)

- Start from composition root (`Program.cs`) in each service to understand dependencies and middleware order.
- Then read controllers for API contract.
- Then read handlers/services for business logic.
- Finally inspect shared kernel entities/context to understand persistence and tenant rules.

Recommended reading order:
1. `SharedKernel/Data/LicenseDbContext.cs:6`
2. `LicenseService/Program.cs:12`
3. `LicenseService/Controllers/AuthController.cs:26`
4. `LicenseService/Controllers/LicensesController.cs:17`
5. `LicenseService/Handlers/ApplyLicenseHandler.cs:17`
6. `ApiGateway/ocelot.docker.json:2`
7. `LicenseFrontend/Controllers/LicensesController.cs:31`

