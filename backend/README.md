# AI-Powered IT Support Resolution Platform — Starter Kit (Phase 1 + Phase 2)

Ye zip ek **working .NET 8 solution** hai — Domain, Application, Infrastructure, API layers,
JWT auth, Ticket CRUD, **AI-based ticket analysis (Claude API)**, **deterministic Policy Engine**,
aur **human-in-the-loop workflow** (process/approve/reject/escalate) — sab already implemented,
EF Core migrations-ready, Swagger, Serilog aur global exception handling ke sath.

Baaki bacha hua (tests, 50+ evaluation dataset, Docker) — README ke end mein "Phase 3" section
mein bataya gaya hai.

## 0. Tumhare 8GB RAM ke liye zaroori tips (bahut important)

8GB RAM par Visual Studio + SQL Server + Chrome sab ek sath chalana slow ho jayega. Ye karo:

1. **Full SQL Server install mat karo / use mat karo.** Iske bajaye **SQL Server LocalDB** use karo
   — ye Visual Studio ke sath already aata hai (bahut halka, background service nahi chalti jab tak
   query na aaye). Is starter kit ka `appsettings.json` already LocalDB ke liye configured hai:
   `Server=(localdb)\MSSQLLocalDB;...`
2. Agar SSMS chahiye connect karne ke liye to server name mein yahi likho:
   `(localdb)\MSSQLLocalDB`
3. Visual Studio mein sirf **ASP.NET and web development** workload rakho (installer se unnecessary
   workloads uncheck kar do — Mobile, Game dev, Data science mat lo).
4. Kaam karte time Chrome ke bahut saare tabs, Docker Desktop, aur heavy apps band rakho — Docker
   sirf Day 28 (deployment) pe chahiye, abhi mat kholo.
5. Agar Visual Studio bahut slow lage, **VS Code + C# Dev Kit** try karo — bahut lighter hai, aur
   `dotnet` CLI se hi sab kaam ho jata hai.

## 1. Folder structure (already banaya hua hai is zip mein)

```
AIITSupport/
├── AIITSupport.sln            (tumhe khud banana hoga, step 2 dekho)
├── .gitignore
├── README.md
└── src/
    ├── AIITSupport.Domain/
    │   ├── Entities/Entities.cs
    │   ├── Enums/Enums.cs
    │   └── Interfaces/IRepositories.cs
    ├── AIITSupport.Application/
    │   ├── DTOs/Dtos.cs
    │   ├── Interfaces/Interfaces.cs
    │   └── Services/AuthService.cs, TicketService.cs
    ├── AIITSupport.Infrastructure/
    │   ├── Data/AppDbContext.cs
    │   ├── Repositories/Repositories.cs
    │   ├── Authentication/TokenService.cs
    │   ├── AI/AIServiceOptions.cs, AnthropicAIService.cs      ← Phase 2
    │   └── Policies/RuleBasedPolicyService.cs                 ← Phase 2
    └── AIITSupport.API/
        ├── Controllers/AuthController.cs, TicketsController.cs,
        │                AIController.cs, WorkflowController.cs,   ← Phase 2
        │                AuditLogsController.cs                    ← Phase 2
        ├── Middleware/ExceptionHandlingMiddleware.cs
        ├── Program.cs
        ├── appsettings.json
        └── Properties/launchSettings.json
```

Isme abhi `AIITSupport.Tests` project nahi hai — Day 25 pe khud add karoge (guide neeche hai).

## 2. Solution banao aur run karo (step by step)

Terminal (VS Code) ya Developer PowerShell (Visual Studio ke andar) mein, `AIITSupport` folder ke
andar jaake ye commands chalao — internet chahiye pehli baar (NuGet packages download karne ke liye):

```bash
cd AIITSupport

# 1) Solution file banao aur sab projects add karo
dotnet new sln -n AIITSupport
dotnet sln add src/AIITSupport.Domain/AIITSupport.Domain.csproj
dotnet sln add src/AIITSupport.Application/AIITSupport.Application.csproj
dotnet sln add src/AIITSupport.Infrastructure/AIITSupport.Infrastructure.csproj
dotnet sln add src/AIITSupport.API/AIITSupport.API.csproj

# 2) Sab packages restore karo
dotnet restore

# 3) EF Core CLI tool install karo (ek baar hi, globally)
dotnet tool install --global dotnet-ef

# 4) Pehli migration banao (Infrastructure project se, startup project API hai)
cd src/AIITSupport.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../AIITSupport.API

# 5) Database create karo (LocalDB mein) — Program.cs already startup pe
#    auto-migrate karta hai, lekin pehli baar manually bhi chala sakte ho:
dotnet ef database update --startup-project ../AIITSupport.API

# 6) API run karo
cd ../AIITSupport.API
dotnet run
```

Browser mein `http://localhost:5080/swagger` khol ke Swagger UI dikhega — wahan se
`POST /api/auth/register`, `POST /api/auth/login`, aur ticket endpoints test kar sakte ho.

**JWT test karne ka tarika:** Login karke jo `token` milega, Swagger ke "Authorize" button mein
`Bearer <token>` daal do — uske baad Tickets endpoints authorize ho jayenge.

## 3. Kya already kaam kar raha hai (Roadmap ke Day 1–22)

**Phase 1 (Foundation, Auth, CRUD):**
- ✅ Domain entities + enums (Users, Roles, Tickets, AIAnalyses, Policies, Approvals, AuditLogs, etc.)
- ✅ EF Core `AppDbContext` — relationships, unique indexes, seeded Roles (Employee/SupportAgent/Admin)
  aur 8 default TicketCategories
- ✅ Repository pattern (`IUserRepository`, `ITicketRepository`, `IAuditLogRepository`)
- ✅ `POST /api/auth/register`, `POST /api/auth/login` — BCrypt password hashing + JWT issue
- ✅ `POST/GET/PUT /api/tickets` — role-based visibility, ownership check
- ✅ Global exception handling middleware, Serilog, Swagger with JWT "Authorize" button

**Phase 2 (AI Analysis, Policy Engine, Human-in-the-Loop Workflow):**
- ✅ `IAIService` → `AnthropicAIService`: Claude API ko call karta hai, ek strict system
  prompt ke sath jo sirf JSON return karwata hai (`category`, `urgency`, `confidence`,
  `summary`, `recommendation`). Response ko validate karta hai — agar category/urgency
  allowed list se bahar hai, confidence 0–1 ke bahar hai, ya JSON malformed hai, to
  `AIResponseInvalidException` throw hoti hai.
- ✅ **Retry logic**: transient failures (timeout, network error, 5xx) par 2 retries,
  exponential backoff (2s, 4s) ke sath. Sab retries fail ho jaye to ticket
  `AIProcessingFailed` status mein chala jaata hai — silently lost nahi hota.
- ✅ `POST /api/tickets/{id}/analyze` — AI analysis chalata hai, `AIAnalyses` table mein save
  karta hai, ticket status `AIAnalyzed` (ya failure pe `AIProcessingFailed`) set karta hai.
- ✅ `GET /api/tickets/{id}/analysis` — latest AI analysis dekhne ke liye.
- ✅ `IPolicyService` → `RuleBasedPolicyService`: pure deterministic C# rules (AI ka koi role
  nahi ismein) —
  - confidence < 85% → HumanReview
  - category `Security` ya `Access` → hamesha HumanReview (chahe confidence kitna bhi ho)
  - urgency `Critical` → hamesha HumanReview
  - baaki sab → eligible for auto-resolve
- ✅ `POST /api/tickets/{id}/process` — (SupportAgent/Admin only) policy evaluate karta hai,
  ticket ko `HumanReview` ya seedha `Resolved` (auto) mein le jaata hai.
- ✅ `POST /api/tickets/{id}/approve`, `/reject` — (SupportAgent/Admin only) sirf `HumanReview`
  status wali tickets pe kaam karta hai, `Approvals` table mein record banata hai.
- ✅ `POST /api/tickets/{id}/escalate` — ticket owner ya agent/admin, kabhi bhi (terminal states
  chhod ke) escalate kar sakta hai.
- ✅ `GET /api/audit-logs` — (Admin only) har sensitive action (analyze, process, approve,
  reject, escalate) `AuditLogs` table mein already record ho raha hai.

### Poora workflow ek employee/agent ke through (Swagger mein try karo):

1. Employee se login karo → `POST /api/auth/register` (role Employee milega by default)
2. `POST /api/tickets` → naya ticket banao
3. Login karke SupportAgent/Admin role wala user chahiye agent actions ke liye —
   abhi self-registration sirf Employee deta hai, isliye ek Admin user manually
   database mein `UserRoles` table se assign karo (ya mujhe bolo "seed an admin user
   script bana do", main SQL script bana dunga).
4. `POST /api/tickets/{id}/analyze` → AI category/urgency/confidence return karega
5. `POST /api/tickets/{id}/process` (agent/admin) → policy decide karega auto-resolve ya
   human-review
6. Agar HumanReview mila: `POST /api/tickets/{id}/approve` ya `/reject` (agent/admin)
7. `GET /api/audit-logs` (admin) → poori trail dikhegi

## 4. Phase 2 setup — Claude API key chahiye

`analyze` endpoint real Anthropic Claude API ko call karta hai, isliye ek API key chahiye
(console.anthropic.com se free/paid key bana sakte ho — free trial credits milte hain).

**Key ko appsettings.json mein mat likhna** — GitHub par leak ho jayega. User secrets use karo:

```bash
cd src/AIITSupport.API
dotnet user-secrets set "Ai:ApiKey" "sk-ant-api03-...tumhari-key..."
```

Agar abhi test ke liye API key nahi lagana chahte, mujhe bolo — main `IAIService` ka ek
**mock/fake implementation** bhi bana sakta hoon jo random category/confidence return kare,
taaki policy engine aur workflow bina real API cost ke test ho sake.

## 5. Ab yahan se aage kya karna hai (Phase 3 — Roadmap Day 23–30)

Isi conversation mein "Phase 3 bana do" bolo — main:

1. `AIITSupport.Tests` project — xUnit + Moq se unit tests (TicketService, PolicyService,
   AI-response parsing/validation, authorization, workflow state transitions).
2. 50+ ticket evaluation dataset (JSON/CSV) aur ek chhota console script jo classification
   accuracy, escalation accuracy, false/missed escalations measure kare.
3. Admin Policy management APIs (`GET/POST/PUT /api/policies`) taaki rules DB-driven bhi ban
   sakein (abhi hardcoded C# mein hain, jo roadmap khud recommend karta hai).
4. Docker Compose — SQL Server ka Docker container (RAM bachane ke liye local install ki
   zaroorat nahi padegi) + API container.
5. MVC frontend pages (Dashboard, Ticket Details, Human Review screen).

## 5. Common errors jo aayenge (aur unka fix)

| Error | Fix |
|---|---|
| `Cannot open database ... requested by the login` | `dotnet ef database update` phir se chalao |
| `A network-related or instance-specific error` | LocalDB service start nahi hai — `sqllocaldb start MSSQLLocalDB` chalao |
| `Unable to resolve service for type 'AppDbContext'` | `dotnet restore` phir se chalao, project reference check karo |
| JWT `401 Unauthorized` | Swagger mein token ke aage `Bearer ` (space ke sath) likhna mat bhoolo — Swagger khud add kar deta hai agar sirf token paste karo |
| `dotnet ef` command not found | `dotnet tool install --global dotnet-ef` chalao, phir naya terminal kholo |
| `/analyze` returns 503 | Claude API key galat hai ya set nahi hai — `dotnet user-secrets list` se check karo |
| `/process` returns 400 "must be in AIAnalyzed status" | Pehle `/analyze` call karo, uske baad hi `/process` chalega |
| `/approve` ya `/reject` returns 403 | Ye endpoints sirf SupportAgent/Admin role ke liye hain — Employee token se nahi chalenge |

## 6. Git / GitHub par daalne se pehle

```bash
git init
git add .
git commit -m "Phase 1: Domain, Application, Infrastructure, API with JWT auth and Ticket CRUD"
```

`appsettings.json` mein JWT key abhi placeholder hai (`CHANGE_THIS_TO_A_LONG_RANDOM_SECRET...`).
Real secret ke liye `dotnet user-secrets` use karo taaki key GitHub par public na jaye:

```bash
cd src/AIITSupport.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "apni-lambi-random-secret-yahan-daalo-32-chars-se-zyada"
```

---

Agla step: is starter ko run karke dekho, agar koi error aaye to yahi paste kar dena — fix kar
denge. Uske baad "Phase 2" bolo, AI + Policy Engine wala part bhi isi tarah se ready-to-run bana
ke doonga.
