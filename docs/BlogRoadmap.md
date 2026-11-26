# Blog Management REST API — Portfolio-Grade Project Roadmap

**Reference Specification:** `BlogDocumentation.md` (V1.0)
**Target Repository:** `blog-management-rest-api` (Standalone project at `D:\learn-API\BlogManagement`)
**Portfolio Purpose:** Primary backend sample for Fiverr **"ASP.NET Core REST API Development"** gig

---

## 1. Strategic Context & Why This Roadmap Restructures Section 17

`BlogDocumentation.md` defines a solid V1.0 specification (Layered Architecture without redundant Generic Repository abstractions, `AsNoTracking()` + DTO projections, `DeleteBehavior.Restrict`, `ProblemDetails`, `CancellationToken`, and xUnit + Postman testing).

However, executing the original 14-session sequence in Section 17 verbatim creates avoidable rework and hides specification gaps. Because this project will represent your engineering standards to prospective Fiverr clients, this roadmap optimizes the build order and adds client-grade quality gates:

1. **Standalone Isolation:** Built in its own directory (`D:\learn-API\BlogManagement`) and Git repository—completely separate from `Day01-RestApi`—with clean commit history and zero template clutter.
2. **Categories Before Posts:** `BR-01` requires every Post to belong to a valid Category. Building Category CRUD first eliminates manual SQL seeding hacks during Post API testing.
3. **Error Handling Before Endpoints:** Implementing `ProblemDetails` and `ExceptionHandlingMiddleware` before building controllers prevents ad-hoc error responses across early sessions.
4. **Incremental Testing:** Unit tests are written alongside each Service and Helper rather than compressed into a single final session, followed by end-to-end SQLite in-memory integration tests.
5. **Fiverr Buyer Readiness:** Every phase enforces details buyers and technical reviewers inspect on GitHub: clean Swagger/OpenAPI annotations, exportable Postman collections with test assertions, safe `appsettings` secret hygiene, and deterministic `PATCH` / Slug behavior.

---

## 2. Phase 0 — Specification Gaps & Environment Alignment (Resolve Before Session 01)

Before writing code, resolve the contradictions and local environment mismatches identified in `BlogDocumentation.md` (Section 20 requires documenting spec decisions explicitly rather than silently changing behavior):

| Area | Gap / Contradiction in `BlogDocumentation.md` | Recommended Resolution & Action |
| :--- | :--- | :--- |
| **Duplicate Slug Policy** | Section 6.1, 10 & `TC-07` originally specified `409 Conflict` for duplicate Slugs, while Section 7.2 & 9 specified server-side auto-appended unique suffix (`-2`, `-3`). | **Locked in `BlogDocumentation.md`:** Server auto-generates unique suffix on `POST` (`201 Created`, `TC-07`) + catches `DbUpdateException` race condition; `Slug` stays immutable on `PUT`/`PATCH` to preserve SEO URLs; duplicate `Category.Name` returns `409` (`TC-17`). |
| **Missing DTO Contracts** | Section 8 defines only `CreatePostRequest`, `UpdatePostRequest`, `PostResponse`. Category DTOs, `PatchPostRequest`, `PagedResponse<T>`, `PostSummaryResponse`, and `PostQueryParameters` are missing. | Define all missing DTOs upfront in Phase 2/3. Use `PostSummaryResponse` for `GET /api/posts` (matching Section 7.3 JSON without `Content`, `Excerpt`, `UpdatedAt`) to avoid pulling `nvarchar(max)` over the wire. |
| **`PATCH` `null` vs Omitted** | Section 7.6 requires distinguishing omitted fields from explicit `null`, which standard nullable C# properties (`string?`) cannot do in `System.Text.Json`. | Implement a lightweight `Optional<T>` wrapper with a custom `JsonConverter` (or `PatchField<T>`) for `PatchPostRequest` so `{"excerpt": null}` clears `Excerpt` while `{}` leaves it unchanged, and explicit `{"title": null}` returns `400`. |
| **Category Error Codes** | `BR-02` requires unique Category `Name`, but Section 6.2 omits `409 Conflict`. Also `TC-06` expects `400 Bad Request` when `CategoryId` does not exist in DB. | Enforce unique index on `Category.Name` (`409 Conflict` on duplicate). Map missing `CategoryId` in Post `Create`/`Update`/`Patch` to `400 Bad Request` (validation error), while missing route `id` (`/posts/{id}`) returns `404 Not Found`. |
| **Query Validation Rules** | Section 7.3 states invalid query parameters return `400`, but allowed `sortBy` / `sortOrder` values and `pageSize < 1` are not constrained in DTOs. | Add `PostQueryParameters` validation: `pageNumber >= 1`, `1 <= pageSize <= 100`, `sortBy` whitelist (`createdAt`, `title`), `sortOrder` whitelist (`asc`, `desc`). |
| **Local SQL Server Instance** | Section 13 uses `Server=.`, whereas this machine runs SQL Server LocalDB (`MSSQLLocalDB`). | Put `Server=(localdb)\\MSSQLLocalDB;Database=BlogManagementDb;...` in `appsettings.Development.json` and keep `appsettings.json` placeholder-only so local machine details stay out of production config. |
| **Swagger vs OpenAPI Packages** | .NET 9 default template uses `Microsoft.AspNetCore.OpenApi`, while Section 12 specifies `Swashbuckle.AspNetCore`. | Use `Swashbuckle.AspNetCore` exclusively (matching Section 12) for clean Swagger UI screenshots; do not mix both OpenAPI generators. |
| **`dotnet-ef` Tool Version** | Local global `dotnet-ef` is `10.0.3` while project targets EF Core `9.*`. | Verify `dotnet ef` compatibility in Session 01/03; if any mismatch occurs, configure a repo-local `.config/dotnet-tools.json` pinned to `dotnet-ef` `9.*`. |
| **SQLite vs SQL Server Tests** | SQL Server default collation is case-insensitive (`CI`); SQLite default `UNIQUE` and `LIKE` are case-sensitive (`CS`). | Normalize comparisons/slugs deterministically in C# and verify integration tests account for SQLite collation behavior without breaking production SQL Server queries. |

---

## 3. Target Standalone Repository Structure

Keep the repository clean, self-documenting, and structured for client/reviewer inspection:

```text
BlogManagement/                              (Git root: blog-management-rest-api)
├── .gitignore
├── README.md
├── BlogManagement.sln
├── docs/
│   ├── BlogDocumentation.md                 (Copy/link spec for reference)
│   └── screenshots/                         (Swagger UI & Postman test screenshots)
├── postman/
│   ├── BlogManagement.postman_collection.json
│   └── BlogManagement.local.postman_environment.json
├── src/
│   └── BlogManagement.API/
│       ├── Configurations/                  (IEntityTypeConfiguration<T> classes)
│       │   ├── CategoryConfiguration.cs
│       │   └── PostConfiguration.cs
│       ├── Controllers/
│       │   ├── CategoriesController.cs
│       │   └── PostsController.cs
│       ├── Data/
│       │   ├── BlogDbContext.cs
│       │   └── DbSeeder.cs                  (Development-only optional sample seed)
│       ├── DTOs/
│       │   ├── Categories/
│       │   │   ├── CategoryResponse.cs
│       │   │   ├── CreateCategoryRequest.cs
│       │   │   └── UpdateCategoryRequest.cs
│       │   ├── Common/
│       │   │   ├── Optional.cs
│       │   │   └── PagedResponse.cs
│       │   └── Posts/
│       │       ├── CreatePostRequest.cs
│       │       ├── PatchPostRequest.cs
│       │       ├── PostQueryParameters.cs
│       │       ├── PostResponse.cs
│       │       ├── PostSummaryResponse.cs
│       │       └── UpdatePostRequest.cs
│       ├── Exceptions/                      (Domain/App exceptions mapped to ProblemDetails)
│       │   ├── BadRequestException.cs
│       │   ├── ConflictException.cs
│       │   └── NotFoundException.cs
│       ├── Helpers/
│       │   └── SlugGenerator.cs
│       ├── Middleware/
│       │   └── ExceptionHandlingMiddleware.cs
│       ├── Migrations/
│       ├── Models/
│       │   ├── Category.cs
│       │   └── Post.cs
│       ├── Services/
│       │   ├── Interfaces/
│       │   │   ├── ICategoryService.cs
│       │   │   └── IPostService.cs
│       │   └── Implementations/
│       │       ├── CategoryService.cs
│       │       └── PostService.cs
│       ├── appsettings.Development.json
│       ├── appsettings.json
│       ├── BlogManagement.API.csproj
│       └── Program.cs
└── tests/
    └── BlogManagement.Tests/
        ├── Helpers/
        │   ├── OptionalJsonConverterTests.cs
        │   └── SlugGeneratorTests.cs
        ├── Services/
        │   ├── CategoryServiceTests.cs
        │   └── PostServiceTests.cs
        ├── Integration/
        │   ├── CustomWebApplicationFactory.cs
        │   ├── CategoriesEndpointTests.cs
        │   └── PostsEndpointTests.cs
        └── BlogManagement.Tests.csproj
```

---

## 4. Phased Execution Roadmap (12 Focused Sessions)

### Phase 1: Project Foundation, Domain & Database

#### Session 01 — Spec Lock, Standalone Git & Solution Setup
* **Goal:** Establish a clean, standalone .NET 9 solution isolated from `Day01-RestApi` and lock Phase 0 spec decisions.
* **Tasks:**
  1. Resolve and document Phase 0 decisions in `BlogDocumentation.md`.
  2. Create `D:\learn-API\BlogManagement`, initialize Git (`git init`), and add standard `.NET` `.gitignore` (excluding `bin/`, `obj/`, `.vs/`, `appsettings.Development.json` if needed, or keeping `appsettings.Development.json` generic for LocalDB without secrets).
  3. Create `BlogManagement.sln`, `src/BlogManagement.API`, and `tests/BlogManagement.Tests` (xUnit).
  4. Install `Microsoft.EntityFrameworkCore.SqlServer` (`9.*`), `Microsoft.EntityFrameworkCore.Design` (`9.*`), and `Swashbuckle.AspNetCore`; remove template `WeatherForecast` files.
  5. Configure `Swashbuckle.AspNetCore` in `Program.cs` (enable XML documentation comments for portfolio-grade Swagger UI).
* **Exit Criteria:** `dotnet build` and `dotnet test` pass; `/swagger` loads in Development.
* **Commit:** `chore: initialize standalone BlogManagement solution and Swagger setup`

#### Session 02 — Entities, Fluent Configurations & DbContext
* **Goal:** Model `Category` and `Post` with strict SQL Server column constraints and relational integrity.
* **Tasks:**
  1. Create `Category` and `Post` entities (`Models/`).
  2. Implement `CategoryConfiguration` and `PostConfiguration` (`IEntityTypeConfiguration<T>`):
     * `Category`: `Name` (`nvarchar(100)`, required, unique index), `Description` (`nvarchar(500)`, nullable), `CreatedAt` (`datetime2`, required).
     * `Post`: `Title` (`nvarchar(200)`, required), `Slug` (`nvarchar(220)`, required, unique index), `Excerpt` (`nvarchar(500)`, nullable), `Content` (`nvarchar(max)`, required), `CreatedAt` (`datetime2`, required), `UpdatedAt` (`datetime2`, nullable).
     * Relationship: One-to-Many with `DeleteBehavior.Restrict` (`BR-07`).
  3. Create `BlogDbContext` applying configurations via `ApplyConfigurationsFromAssembly`.
  4. Configure `DefaultConnection` in `appsettings.Development.json` for `(localdb)\MSSQLLocalDB`.
* **Exit Criteria:** Project compiles cleanly; EF Core design-time model validates without warnings.
* **Commit:** `feat(data): add Category and Post entities with EF Core configurations`

#### Session 03 — Initial Migration & SQL Server Verification
* **Goal:** Generate and apply the initial database schema to SQL Server LocalDB.
* **Tasks:**
  1. Ensure `MSSQLLocalDB` is running (`sqllocaldb start MSSQLLocalDB`).
  2. Run `dotnet ef migrations add InitialCreate` and inspect the generated migration file (`nvarchar` lengths, unique indexes, `Restrict` FK constraint).
  3. Run `dotnet ef database update` to create `BlogManagementDb`.
  4. Verify `dotnet-ef` (`10.0.3` vs `9.*`) compatibility cleanly.
* **Exit Criteria:** `BlogManagementDb` exists in LocalDB with `Categories`, `Posts`, `__EFMigrationsHistory`, unique indexes (`IX_Categories_Name`, `IX_Posts_Slug`), and `FK_Posts_Categories_CategoryId` (`NO ACTION`).
* **Commit:** `feat(data): add InitialCreate EF Core migration`

---

### Phase 2: Contracts, Error Architecture & Categories Module

#### Session 04 — Common Contracts, Exceptions & Global Error Handling
* **Goal:** Guarantee RFC 9110 `ProblemDetails` consistency across all endpoints before writing business logic.
* **Tasks:**
  1. Create `PagedResponse<T>` (`DTOs/Common/`).
  2. Create typed exceptions (`NotFoundException`, `ConflictException`, `BadRequestException`).
  3. Implement `ExceptionHandlingMiddleware` mapping:
     * `BadRequestException` -> `400 Bad Request`
     * `NotFoundException` -> `404 Not Found`
     * `ConflictException` -> `409 Conflict`
     * `DbUpdateException` (SQL Server unique constraint `2601`/`2627` or SQLite `SQLITE_CONSTRAINT_UNIQUE`) -> `409 Conflict`
     * Unhandled exceptions -> `500 Internal Server Error` (log full exception server-side via `ILogger`; never expose stack traces or connection strings in non-Development).
  4. Configure `[ApiController]` `InvalidModelStateResponseFactory` (or built-in `AddProblemDetails`) so validation errors match the exact `ProblemDetails` contract in Section 10.
* **Exit Criteria:** Triggering simulated exceptions returns clean `application/problem+json` responses with accurate status codes and zero leaked internals.
* **Commit:** `feat(api): add ProblemDetails global exception handling and PagedResponse`

#### Session 05 — Category DTOs, Service Layer & Unit Tests
* **Goal:** Implement Category business logic (`FR-07`..`FR-11`, `BR-02`, `BR-07`) with isolated unit test coverage.
* **Tasks:**
  1. Create `CreateCategoryRequest`, `UpdateCategoryRequest`, and `CategoryResponse` (`DTOs/Categories/`).
  2. Implement `ICategoryService` and `CategoryService`:
     * `GetAllAsync`, `GetByIdAsync` (`AsNoTracking()`, `Select()` projection, `CancellationToken`).
     * `CreateAsync` (duplicate `Name` -> `ConflictException`, `CreatedAt = DateTime.UtcNow`).
     * `UpdateAsync` (missing id -> `NotFoundException`, duplicate `Name` on another id -> `ConflictException`).
     * `DeleteAsync` (missing id -> `NotFoundException`, existing posts -> `ConflictException` before delete + DB restrict safety).
  3. Write `CategoryServiceTests` in `BlogManagement.Tests` covering valid flows, duplicate names, missing IDs, and delete-with-posts rejection (`TC-16`).
* **Exit Criteria:** All `CategoryServiceTests` pass via `dotnet test`.
* **Commit:** `feat(categories): implement CategoryService, DTOs, and unit tests`

#### Session 06 — CategoriesController & Swagger/OpenAPI Polish
* **Goal:** Expose Category REST endpoints with clean HTTP semantics and client-ready Swagger docs.
* **Tasks:**
  1. Implement `CategoriesController` (`GET`, `GET {id}`, `POST` with `CreatedAtAction` `201`, `PUT` `204`, `DELETE` `204`).
  2. Add XML doc summaries and `[ProducesResponseType]` attributes for `200`, `201`, `204`, `400`, `404`, `409`.
  3. Pass `CancellationToken` through every controller action.
* **Exit Criteria:** Full Category CRUD + `400`/`404`/`409` cases verified via Swagger UI / `.http` file.
* **Commit:** `feat(categories): add CategoriesController with OpenAPI response metadata`

---

### Phase 3: Posts Module, Slug Engine & Query Pipeline

#### Session 07 — Post DTOs, `Optional<T>` PATCH Design & Slug Generator
* **Goal:** Build rock-solid Post contracts and pure-logic helpers before touching Post persistence.
* **Tasks:**
  1. Implement `Optional<T>` + `OptionalJsonConverterFactory` (`DTOs/Common/`) so `PatchPostRequest` strictly differentiates omitted fields vs explicit `null`.
  2. Create `CreatePostRequest`, `UpdatePostRequest`, `PatchPostRequest`, `PostResponse`, `PostSummaryResponse`, and `PostQueryParameters` (with `IValidatableObject` or Data Annotations enforcing `pageNumber >= 1`, `1 <= pageSize <= 100`, `sortBy` in `{createdAt, title}`, `sortOrder` in `{asc, desc}`).
  3. Implement `SlugGenerator` (`Helpers/`): lowercase, normalize punctuation/symbols (e.g., `ASP.NET Core` -> `asp-net-core`, `C# & .NET` -> `c-net`), collapse hyphens, trim edges, respect max length (`220` chars with suffix room), fallback when title has no alphanumeric chars.
  4. Write `SlugGeneratorTests` and `OptionalJsonConverterTests` (including omitted vs `null` vs invalid values).
* **Exit Criteria:** All helper and `PATCH` deserialization unit tests pass.
* **Commit:** `feat(posts): add Post DTOs, Optional<T> PATCH converter, and SlugGenerator`

#### Session 08 — Post Service Core (CRUD, Validation & Unit Tests)
* **Goal:** Implement Post CRUD (`FR-01`..`FR-06`, `BR-01`, `BR-03`..`BR-06`, `BR-08`) with clean error boundaries.
* **Tasks:**
  1. Implement `IPostService` and `PostService` methods: `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `PatchAsync`, `DeleteAsync`.
  2. Enforce `CategoryId` existence check -> throw `BadRequestException` (`400` per `TC-06`).
  3. Enforce locked Slug rule (server-generated unique slug / conflict rule) + `CreatedAt` / `UpdatedAt` UTC timestamps.
  4. Enforce `PatchAsync` validation on explicit `null` for required fields (`Title`, `Content`, `CategoryId`) while allowing `Excerpt` to be cleared via `{"excerpt": null}`.
  5. Write `PostServiceTests` covering `TC-02`..`TC-12` logic.
* **Exit Criteria:** All `PostService` CRUD unit tests pass.
* **Commit:** `feat(posts): implement PostService CRUD logic and unit tests`

#### Session 09 — Search, Filtering, Pagination & Sorting Query Pipeline
* **Goal:** Implement scalable database-side querying (`FR-12`..`FR-15`, `BR-09`, `BR-10`, Section 14).
* **Tasks:**
  1. Implement `GetAllAsync(PostQueryParameters, CancellationToken)` returning `PagedResponse<PostSummaryResponse>`.
  2. Build composable `IQueryable<Post>` pipeline with `AsNoTracking()`:
     * Search filter on `Title` and `Content` (`EF.Functions.Like` or `.Contains` evaluated in SQL).
     * Optional `categoryId` filter.
     * Dynamic switch-expression sorting (`createdAt` / `title`, `asc` / `desc` — safe against SQL injection, no reflection hacks).
     * Execute `CountAsync` before `Skip`/`Take`, then project directly via `Select()` into `PostSummaryResponse` (never fetching `Content` in list responses).
  3. Write unit tests covering `TC-13`, `TC-14`, `TC-15`, empty pages, boundary `pageSize = 100`, and sort orders.
* **Exit Criteria:** All querying/pagination unit tests pass; inspect EF Core SQL logs in Development to confirm single-projection SQL without `N+1` or `Content` selection.
* **Commit:** `feat(posts): add DB-level search, filtering, sorting, and pagination`

#### Session 10 — PostsController & Full Manual API Verification
* **Goal:** Expose all Post REST endpoints (`FR-01`..`FR-06`, `FR-12`..`FR-15`) and validate end-to-end against LocalDB.
* **Tasks:**
  1. Implement `PostsController` (`GET`, `GET {id}`, `POST` with `201` + `Location`, `PUT` `204`, `PATCH` `204`, `DELETE` `204`).
  2. Add comprehensive XML summaries, parameter descriptions, and `[ProducesResponseType]` attributes for Swagger UI.
  3. Optionally add a Development-only seed helper (guarded by `app.Environment.IsDevelopment()`) so demoing/testing works right after startup.
* **Exit Criteria:** Manually verify all 16 Functional Test Cases (`TC-01`..`TC-16`) against SQL Server LocalDB via Swagger UI.
* **Commit:** `feat(posts): add PostsController and complete Swagger documentation`

---

### Phase 4: Automated Integration Testing, Postman & Fiverr Portfolio Packaging

#### Session 11 — Integration Tests (`WebApplicationFactory` + SQLite In-Memory)
* **Goal:** Automate full HTTP-to-Database verification (`TC-01`..`TC-17`) with zero mocked controllers.
* **Tasks:**
  1. Add `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.Sqlite` to `BlogManagement.Tests`.
  2. Create `CustomWebApplicationFactory` keeping an open SQLite `:memory:` connection per test fixture, applying schema via `EnsureCreated()`, and verifying `DeleteBehavior.Restrict` actually enforces FK constraints.
  3. Write integration test suites (`CategoriesEndpointTests`, `PostsEndpointTests`) covering:
     * All `TC-01` through `TC-17` scenarios
     * Exact `ProblemDetails` JSON fields (`status`, `title`, `errors`)
     * `PATCH` omitted vs explicit `null` (`Excerpt` cleared vs preserved, `Title: null` rejected with `400`)
     * Query validation errors (`pageNumber=0`, `pageSize=101`, `sortBy=invalid`)
     * Lock key architectural invariants with automated regression assertions (e.g., max `pageSize` = `100`, allowed `sortBy` count).
* **Exit Criteria:** `dotnet test` executes full Unit + Integration suite with 100% passing assertions and zero skipped tests.
* **Commit:** `test(integration): add WebApplicationFactory SQLite tests for TC-01..TC-16`

#### Session 12 — Postman Collection, Screenshots, README & GitHub Release
* **Goal:** Turn the finished codebase into a conversion-ready Fiverr portfolio showcase.
* **Tasks:**
  1. **Postman Deliverables (`postman/`):** Create a clean Postman collection grouped into `Categories` and `Posts`, add Postman test scripts (`pm.test(...)` checking status codes and JSON fields), export collection + local environment JSON (with `baseUrl = https://localhost:7080/api`).
  2. **Visual Proof (`docs/screenshots/`):** Capture crisp, high-resolution screenshots (light/dark readable, clean data) of:
     * Swagger UI overview (all endpoints grouped)
     * Swagger `POST /api/posts` request + `201` response
     * `GET /api/posts` paginated/filtered JSON response
     * `400`/`409` `ProblemDetails` error response
     * Postman Collection Runner showing all green assertions
  3. **Portfolio `README.md`:** Write all 15 sections from `BlogDocumentation.md` Section 16, adding:
     * Clear **Architectural Highlights** (why `DbContext` directly, `Select()` projections, `Optional<T>` PATCH, `ProblemDetails`, `AsNoTracking()`, `CancellationToken`)
     * Embedded ER diagram / Architecture diagram (Mermaid)
     * Embedded screenshots from `docs/screenshots/`
     * Explicit **Version 2.0 Roadmap** (JWT Auth, RBAC, Caching, Rate Limiting) — acts as a natural upsell preview for Fiverr clients needing extended scope
     * Honest attribution as a self-built Portfolio / Reference Architecture project (Section 16 rule)
  4. **Security & Clean Repo Audit:** Verify no secrets/credentials exist in Git history, run a fresh build + test (`dotnet restore && dotnet build && dotnet test`), and push to GitHub (`blog-management-rest-api`).
* **Exit Criteria:** A stranger can clone `blog-management-rest-api`, run migrations, import the Postman collection, pass `dotnet test`, and inspect Swagger UI without guessing any setup step.
* **Commit:** `docs: add Postman collection, screenshots, and portfolio README`

---

## 5. Requirement & Test Case Traceability Matrix

| Spec Group | Covered In | Verification Method |
| :--- | :--- | :--- |
| `FR-01`..`FR-06` (Posts CRUD + PATCH) | Sessions 07, 08, 10 | Unit + Integration + Postman |
| `FR-07`..`FR-11` (Categories CRUD) | Sessions 05, 06 | Unit + Integration + Postman |
| `FR-12`..`FR-15` (Search, Filter, Page, Sort) | Sessions 09, 10 | Unit + Integration (`TC-13`..`TC-15`) |
| `FR-16`, `FR-17` (Validation & `ProblemDetails`) | Sessions 04, 07, 08 | Integration (`TC-05`, `TC-06`, `400`/`404`/`409`/`500`) |
| `BR-01`..`BR-10` (Business Rules) | Sessions 02, 05, 07, 08, 09 | DB Constraints + Service Unit + Integration Tests |
| `TC-01`..`TC-17` (Functional Test Cases) | Sessions 05, 08, 09, 11, 12 | xUnit Integration Tests + Postman Runner |

---

## 6. Fiverr Gig Portfolio Presentation Checklist

When attaching this project to your Fiverr **"ASP.NET Core REST API Development"** gig:

* **Gig Gallery / PDF Case Study:** Use the Swagger overview screenshot, Postman green test-runner screenshot, and a clean architecture/DB diagram slide (clients rarely read raw code first—visual clarity wins clicks).
* **Live GitHub Link / Code Sample:** Ensure the repo description matches Section 16 (`A RESTful Blog Management API built with ASP.NET Core 9, Entity Framework Core 9, and Microsoft SQL Server.`) and pin the repo on your GitHub profile.
* **Key Selling Points to Highlight in Project Description:**
  * Clean Layered Architecture with EF Core 9 & SQL Server (`DeleteBehavior.Restrict`, Unique Indexes)
  * Database-level Search, Filtering, Sorting & Pagination (`AsNoTracking` + DTO projections)
  * RFC 9110 `ProblemDetails` global error handling & `Optional<T>` partial updates (`PATCH`)
  * Automated xUnit Unit + SQLite Integration Tests + Ready-to-run Postman Collection & Swagger UI
  * Honest framing: present clearly as a self-built backend reference/portfolio project (`V1.0` core API, ready to extend with `V2.0` JWT Auth & Role-Based Access).
