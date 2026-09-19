# 🤖 AI-Powered IT Support Resolution Platform (Backend)

An enterprise-style **AI-powered IT Support Resolution Platform** built with **ASP.NET Core 8**, **Clean Architecture**, **JWT Authentication**, **Entity Framework Core**, and an **AI-assisted ticket analysis workflow**.

The backend provides secure authentication, ticket management, AI-based ticket classification, rule-based policy evaluation, human approval workflow, and audit logging.

---

## 🚀 Features

* 🔐 JWT Authentication & Role-Based Authorization
* 👤 Employee, Support Agent, and Admin roles
* 🎫 Ticket CRUD (Create, Update, View, Escalate)
* 🤖 AI-powered Ticket Classification (Category, Urgency, Recommendation)
* 📋 Rule-Based Policy Engine for automatic or human review
* ✅ Human-in-the-Loop Approval Workflow
* 📝 Audit Logging for sensitive operations
* 🛡 Global Exception Handling Middleware
* 📚 Swagger API Documentation
* 🗃 Entity Framework Core with SQL Server LocalDB

---

## 📸 Project Preview

### Dashboard

![Dashboard](screenshots/dashboard.png)

### Swagger API

![Swagger API](screenshots/swagger-api.png)

### Create Ticket

![Create Ticket](screenshots/create-ticket.png)

### Ticket Details

![Ticket Details](screenshots/ticket-details.png)

### Human Review Workflow

![Workflow](screenshots/workflow.png)

---

## 🏗 Architecture

This project follows **Clean Architecture**.

```text
Backend
│
├── AIITSupport.API              # REST API & Controllers
├── AIITSupport.Application      # Business Logic & Services
├── AIITSupport.Domain           # Entities & Interfaces
├── AIITSupport.Infrastructure   # Database, AI, JWT, Repositories
└── Tests                        # Unit & Evaluation Tests
```

### Architecture Layers

| Layer          | Responsibility                                       |
| -------------- | ---------------------------------------------------- |
| API            | Controllers, Authentication, Swagger, Middleware     |
| Application    | Business services, DTOs, Interfaces                  |
| Domain         | Entities, Enums, Repository Contracts                |
| Infrastructure | EF Core, JWT, AI Service, Repository Implementations |

---

## 🛠 Tech Stack

| Technology            | Purpose            |
| --------------------- | ------------------ |
| ASP.NET Core 8        | REST API Framework |
| C#                    | Backend Language   |
| Entity Framework Core | ORM                |
| SQL Server LocalDB    | Database           |
| JWT Authentication    | Secure Login       |
| Swagger               | API Testing        |
| Serilog               | Logging            |
| BCrypt                | Password Hashing   |

---

## 📁 Project Structure

```text
backend/
│
├── src/
│   ├── AIITSupport.API/
│   ├── AIITSupport.Application/
│   ├── AIITSupport.Domain/
│   └── AIITSupport.Infrastructure/
│
├── tests/
├── screenshots/
├── README.md
└── .gitignore
```

---

## ⚙️ Getting Started

### 1. Clone Repository

```bash
git clone https://github.com/ManuDangi/AI-Powered-IT-Support-Platform.git
cd AI-Powered-IT-Support-Platform/backend
```

### 2. Create Solution

```bash
dotnet new sln -n AIITSupport

dotnet sln add src/AIITSupport.Domain/AIITSupport.Domain.csproj
dotnet sln add src/AIITSupport.Application/AIITSupport.Application.csproj
dotnet sln add src/AIITSupport.Infrastructure/AIITSupport.Infrastructure.csproj
dotnet sln add src/AIITSupport.API/AIITSupport.API.csproj
```

### 3. Restore Packages

```bash
dotnet restore
```

### 4. Install EF CLI

```bash
dotnet tool install --global dotnet-ef
```

### 5. Create Database

```bash
cd src/AIITSupport.Infrastructure

dotnet ef migrations add InitialCreate \
  --startup-project ../AIITSupport.API

dotnet ef database update \
  --startup-project ../AIITSupport.API
```

### 6. Run API

```bash
cd ../AIITSupport.API
dotnet run
```

Swagger will be available at:

```text
http://localhost:5080/swagger
```

---

## 🔐 Authentication

### Register

`POST /api/auth/register`

### Login

`POST /api/auth/login`

Returns a JWT token.

Use the token inside Swagger Authorization.

```text
Bearer YOUR_TOKEN
```

---

## 🎫 Ticket Workflow

1. Employee creates a ticket.
2. AI analyzes the ticket.
3. Policy Engine evaluates confidence and urgency.
4. Ticket is automatically resolved **or** sent for human review.
5. Support Agent/Admin approves, rejects, or escalates.
6. Every action is recorded in Audit Logs.

---

## 🤖 AI Ticket Analysis

The AI service analyzes ticket descriptions and returns:

* Category
* Urgency
* Confidence Score
* Summary
* Recommended Resolution

The response is validated before saving to the database.

---

## 📋 Rule-Based Policy Engine

Business rules decide whether a ticket requires human approval.

Examples include:

* Critical urgency requires review.
* Security or Access tickets require review.
* Low AI confidence requires review.
* High-confidence standard tickets may be auto-resolved.

---

## 📚 API Endpoints

### Authentication

| Method | Endpoint             |
| ------ | -------------------- |
| POST   | `/api/auth/register` |
| POST   | `/api/auth/login`    |

### Tickets

| Method | Endpoint                     |
| ------ | ---------------------------- |
| GET    | `/api/tickets`               |
| POST   | `/api/tickets`               |
| PUT    | `/api/tickets/{id}`          |
| POST   | `/api/tickets/{id}/analyze`  |
| POST   | `/api/tickets/{id}/process`  |
| POST   | `/api/tickets/{id}/approve`  |
| POST   | `/api/tickets/{id}/reject`   |
| POST   | `/api/tickets/{id}/escalate` |

### Audit Logs

| Method | Endpoint          |
| ------ | ----------------- |
| GET    | `/api/audit-logs` |

---

## 🧪 Testing

Unit tests are available inside:

```text
tests/
```

Run all tests:

```bash
dotnet test
```

---

## 🔒 Environment Variables

Store secrets using **.NET User Secrets** instead of `appsettings.json`.

Initialize secrets:

```bash
cd src/AIITSupport.API

dotnet user-secrets init
```

Add JWT Secret:

```bash
dotnet user-secrets set "Jwt:Key" "YOUR_SECRET_KEY"
```

Add AI API Key:

```bash
dotnet user-secrets set "Ai:ApiKey" "YOUR_AI_API_KEY"
```

---

## 📌 Future Improvements

* Docker & Docker Compose support.
* Admin policy management UI.
* AI evaluation dataset.
* Notification service.
* React/Angular frontend integration.
* CI/CD with GitHub Actions.

---

## 👨‍💻 Author

**Manu Dangi**

GitHub: https://github.com/ManuDangi
