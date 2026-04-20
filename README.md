# Multi-Tenant License Management System

A microservices-based application built with .NET 8 to manage professional licenses across multiple government agencies with strict multi-tenancy isolation.

## Project Overview
This project demonstrates a scalable microservices architecture for license management. It incorporates modern design patterns including CQRS, API Gateway, and background processing.

### Requirement Fulfillment
| Requirement | Implementation Detail |
| :--- | :--- |
| **ASP.NET MVC Frontend** | `LicenseFrontend` project provides role-based dashboards for Applicants, Agencies, and Admins. |
| **Microservices (4)** | `LicenseService`, `DocumentService`, `NotificationService`, and `PaymentService`. |
| **Multi-Tenancy** | SQL Server isolation using `TenantId` with EF Core Global Query Filters for secure data segregation. |
| **RESTful APIs & JWT** | All services expose RESTful endpoints protected by JWT authentication and role-based authorization. |
| **CQRS Pattern** | Implemented in `LicenseService` using **MediatR** for license application workflows. |
| **Background Jobs** | **Hangfire** integration in `LicenseService` for automated license renewal processing. |
| **API Gateway** | **Ocelot** gateway (`ApiGateway` project) acts as the single entry point, routing requests to downstream services. |
| **Dockerization** | All services are fully containerized with production-ready multi-stage Dockerfiles. |
| **Unit Testing** | xUnit tests for core CQRS handlers in `LicenseSystem.Tests`. |

## Project Structure
- `ApiGateway`: Ocelot-based entry point for all API requests.
- `LicenseFrontend`: ASP.NET Core MVC application with role-based UI.
- `LicenseService`: Core service handling license logic, CQRS, and background jobs.
- `DocumentService`: Manages secure document uploads and storage.
- `NotificationService`: Handles user notifications and alerts.
- `PaymentService`: Mock service for license fee processing.
- `SharedKernel`: Shared models, DTOs, and common logic.
- `LicenseSystem.Tests`: Unit tests for key business logic using xUnit and Moq.

## Design Rationale
- **Microservices**: Decoupled services allow independent scaling and technology choices for different business domains.
- **CQRS**: Separating commands (Apply, Update) from queries (List, View) improves performance and maintainability of the complex license workflow.
- **Multi-Tenancy**: The system uses a shared database with discriminator columns (`TenantId`), ensuring that agencies can only access their own data.
- **API Gateway**: Provides a unified API surface, handles cross-cutting concerns like authentication, and simplifies frontend communication.

## Setup Instructions

### 1. Local Development (Standard)
This is the default configuration for running services directly on your machine.
1. **Database**: Create a SQL Server database named `LicenseDb`.
2. **Migrations**: Run EF Core migrations:
   ```bash
   dotnet ef database update --project LicenseService
   ```
3. **Run Services**: Start each microservice and the frontend. Ensure the **ApiGateway** is running on port 5000.
4. **Access**: Open `http://localhost:5005` in your browser.

---

### 2. Running with Docker (Containerized)
The project includes `Dockerfile` files for each service and a `docker-compose.yml`. To run the system in Docker, follow these configuration steps:

#### Required Configuration Changes for Docker:
To allow containers to communicate within the Docker network, you must update the following files:

1. **ApiGateway/ocelot.json**:
   - Change `Host: "localhost"` to the service names defined in `docker-compose.yml` (e.g., `"Host": "license-service"`).
   - Change `Port` values to `80` (the internal container port).

2. **Frontend Controllers**:
   - Refactor hardcoded `http://localhost:[PORT]` URLs to use a configurable `GatewayUrl`.
   - In `appsettings.json`, set `GatewayUrl` to `http://api-gateway`.

#### Starting Docker:
1. Ensure Docker Desktop is running.
2. Execute:
   ```bash
   docker-compose up --build
   ```
3. The system will be available at `http://localhost:5005`.

---

## Usage Guide

The system supports three primary roles: **Applicant**, **Agency (State)**, and **Admin**. 

### 1. User Registration
- Navigate to the **Register** page.
- Choose a **Tenant ID** (e.g., `agency1`). Users within the same Tenant ID share the same database isolation.
- Select your **Role**:
    - `Applicant`: For individuals applying for licenses.
    - `Agency`: For government officials reviewing applications.

### 2. License Application Workflow (Applicant)
1. **Login** as an Applicant using your credentials and Tenant ID.
2. From the **Dashboard**, click **Apply for New License**.
3. Fill in the **Applicant Name**, **Target Agency** (e.g., "Health Department"), and upload a supporting document (e.g., a PDF of your ID).
4. Click **Submit Application**.
5. The system will process the document upload, create the license record, process a mock payment, and send you a notification.
6. Your application will appear in your dashboard with the status `Pending`.

### 3. Review & Approval Workflow (Agency)
1. **Login** as an Agency user using the **same Tenant ID** as the applicant.
2. Your **Agency Dashboard** will display all applications submitted within your tenant.
3. Locate the `Pending` application.
4. (Optional) Click **Download** to review the uploaded document.
5. Click **Approve** or **Reject**.
6. The license status will update instantly, and a notification will be sent to the Applicant's dashboard.

---

## Future AWS Deployment
The project is container-ready and can be deployed to **AWS ECS (Fargate)** or **EKS**.
- **Database**: Use **AWS RDS SQL Server Express** (Free Tier).
- **Images**: Push your Docker images to **Amazon ECR**.
- **Load Balancing**: Use an **Application Load Balancer (ALB)** to route traffic through the Ocelot ApiGateway.