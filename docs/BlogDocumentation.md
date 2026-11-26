# **Project-01: Blog Management REST API — Complete Documentation**

The objective of this project is not merely to build a CRUD API. Instead, it is to step-by-step implement **ASP.NET Core Web API, SQL Server, Entity Framework Core, RESTful API Design, Validation, Error Handling, Testing, and Documentation**—just like a real-world client project.

We will build this as your first Backend Project for your Fiverr Portfolio. This documentation will serve as our primary reference during development; we will strictly follow this specification in subsequent implementations.

# **Blog Management REST API**

| Subject | Specified Technology |
| ----- | ----- |
| Framework | ASP.NET Core 9 Web API |
| Language | C\# |
| Database | Microsoft SQL Server |
| ORM | Entity Framework Core 9 |
| API Documentation | Swagger / OpenAPI |
| API Testing | Postman |
| IDE | Visual Studio Code |
| Version Control | Git & GitHub |
| Unit Testing | xUnit |

Technology note: Official support for .NET 9 ended on November 10, 2026\. We will proceed with your selected .NET 9 for this learning project; for new production client projects, it is recommended to use .NET 10 LTS.

## **1\. Project Overview**

**Blog Management REST API** is a backend application designed to manage Blog Categories and Posts.

Through the API, a user can:

* View all Blog Posts.  
* View a specific Post.  
* Create a new Post.  
* Update an existing Post.  
* Partially modify specific fields of a Post.  
* Delete a Post.  
* Create and manage Categories.  
* Use Search, Filtering, Pagination, and Sorting.

The initial version of this project will not include Authentication or a Frontend. These will be added as future extensions so that we can thoroughly master core REST API development first.

## **2\. Project Objectives**

Upon completing this project, you should acquire the following capabilities:

1. Understand RESTful API architecture and design Endpoints.  
2. Build ASP.NET Core Web API Controllers.  
3. Design C\# Entities and DTOs.  
4. Connect to SQL Server Database using EF Core.  
5. Create and manage Database Migrations.  
6. Implement CRUD operations.  
7. Apply request validation and proper HTTP status codes.  
8. Implement Search, Filtering, Pagination, and Sorting.  
9. Utilize Async programming and CancellationToken.  
10. Test, debug APIs, and generate Swagger documentation.  
11. Publish the project on GitHub with a portfolio-ready README.

## **3\. Scope & Functional Requirements**

### **3.1 Post Management**

| ID | Requirement | Description |
| ----- | ----- | ----- |
| FR-01 | Get All Posts | Retrieve all Posts |
| FR-02 | Get Post by ID | Retrieve a specific Post |
| FR-03 | Create Post | Create a new Post |
| FR-04 | Update Post | Update a Post completely |
| FR-05 | Partial Update | Update specific fields |
| FR-06 | Delete Post | Delete a Post |

### **3.2 Category Management**

| ID | Requirement | Description |
| ----- | ----- | ----- |
| FR-07 | Get Categories | Retrieve all Categories |
| FR-08 | Get Category by ID | Retrieve a specific Category |
| FR-09 | Create Category | Create a new Category |
| FR-10 | Update Category | Update a Category |
| FR-11 | Delete Category | Delete a Category |

### **3.3 Additional Features**

| ID | Feature | Description |
| ----- | ----- | ----- |
| FR-12 | Search | Search Posts by Title or Content |
| FR-13 | Filtering | Filter Posts by Category |
| FR-14 | Pagination | Results by Page and Page Size |
| FR-15 | Sorting | Sort by CreatedAt or Title |
| FR-16 | Validation | Reject invalid input |
| FR-17 | Error Handling | Consistent API error response |

## **4\. System Architecture**

We will use a simple yet maintainable **Layered Architecture**.

**Client Layer**

Postman / API Consumer

**Presentation Layer**

ASP.NET Core Controllers · HTTP Request / Response

**Service Layer**

Business Logic · Validation Rules · DTO Mapping

**Data Access Layer**

EF Core · DbContext · LINQ

**Database Layer**

Microsoft SQL Server

**Layer Responsibilities:**

* **Controller:** Receives HTTP requests, handles responses, and determines status codes.  
* **Service:** Contains business rules and application logic.  
* **Data Access:** Handles Database operations via EF Core.  
* **DTO:** Defines what Data is accepted and returned by the API.  
* **Entity:** Defines the Database data structure.

In this project, we will not create a separate Generic Repository or redundant abstractions. We will use EF Core's `DbContext` directly as the Data Access abstraction.

## **5\. Database Design**

Database name: `BlogManagementDb`

Our Database will contain two main tables in the first version.

### **5.1 Categories Table**

| Column | Data Type | Constraint |
| ----- | ----- | ----- |
| Id | int | Primary Key, Identity |
| Name | nvarchar(100) | Required, Unique |
| Description | nvarchar(500) | Nullable |
| CreatedAt | datetime2 | Required |

### **5.2 Posts Table**

| Column | Data Type | Constraint |
| ----- | ----- | ----- |
| Id | int | Primary Key, Identity |
| Title | nvarchar(200) | Required |
| Slug | nvarchar(220) | Required, Unique |
| Excerpt | nvarchar(500) | Nullable |
| Content | nvarchar(max) | Required |
| CategoryId | int | Foreign Key |
| CreatedAt | datetime2 | Required |
| UpdatedAt | datetime2 | Nullable |

### **5.3 Entity Relationship**

**Categories**

PK · Id

Name · Description · CreatedAt

1

One-to-Many

N

**Posts**

PK · Id · FK · CategoryId

Title · Slug · Excerpt · Content · CreatedAt · UpdatedAt

**Relationship:** A Category can have multiple Posts under it. However, each Post belongs to exactly one Category.

**Delete Rule:** A Category cannot be deleted if there are Posts associated with it. We will configure `DeleteBehavior.Restrict` in the database relationship.

## **6\. API Endpoint Specification**

Base URL:

```
https://localhost:7080/api
```

### **6.1 Posts API**

| Method | Endpoint | Success Status |
| ----- | ----- | ----- |
| GET | `/posts` | 200 |
| GET | `/posts/{id}` | 200 |
| POST | `/posts` | 201 |
| PUT | `/posts/{id}` | 204 |
| PATCH | `/posts/{id}` | 204 |
| DELETE | `/posts/{id}` | 204 |

Error cases:

* If resource is not found: `404 Not Found`  
* If request is invalid (or referenced `CategoryId` does not exist): `400 Bad Request`  
* If an unresolvable database unique-constraint conflict occurs: `409 Conflict`  
* If unexpected server error occurs: `500 Internal Server Error`

### **6.2 Categories API**

| Method | Endpoint | Success Status |
| ----- | ----- | ----- |
| GET | `/categories` | 200 |
| GET | `/categories/{id}` | 200 |
| POST | `/categories` | 201 |
| PUT | `/categories/{id}` | 204 |
| DELETE | `/categories/{id}` | 204 |

Error cases:

* Attempting to create or update a Category with a duplicate `Name` will return `409 Conflict`.  
* Attempting to delete a Category with existing Posts will return `409 Conflict`.  
* If Category is not found: `404 Not Found`  
* If request is invalid: `400 Bad Request`

## **7\. API Request & Response Design**

This section specifies the Request input type and expected Response for each Endpoint.

### **7.1 Create Category**

POST  
`/api/categories`

Request Body:

```
{
  "name": "ASP.NET Core",
  "description": "Articles about ASP.NET Core development."
}
```

Success Response: `201 Created`

```
{
  "id": 1,
  "name": "ASP.NET Core",
  "description": "Articles about ASP.NET Core development.",
  "createdAt": "2026-11-26T10:00:00Z"
}
```

### **7.2 Create Post**

POST  
`/api/posts`

Request Body:

```
{
  "title": "Getting Started with ASP.NET Core",
  "excerpt": "An introduction to ASP.NET Core Web API.",
  "content": "ASP.NET Core is a modern framework for building web applications and APIs.",
  "categoryId": 1
}
```

Success Response: `201 Created`

```
{
  "id": 1,
  "title": "Getting Started with ASP.NET Core",
  "slug": "getting-started-with-asp-net-core",
  "excerpt": "An introduction to ASP.NET Core Web API.",
  "content": "ASP.NET Core is a modern framework for building web applications and APIs.",
  "categoryId": 1,
  "categoryName": "ASP.NET Core",
  "createdAt": "2026-11-26T10:00:00Z",
  "updatedAt": null
}
```

`Slug` will be generated server-side from `Title`. The Client does not need to send a Slug. If a normalized Slug already exists, the server automatically appends a unique numeric suffix (`-2`, `-3`, ...) so `POST /api/posts` succeeds with `201 Created`.

### **7.3 Get All Posts**

GET  
`/api/posts`

Possible Query Parameters:

| Parameter | Example | Purpose |
| ----- | ----- | ----- |
| search | `search=asp.net` | Search Title or Content |
| categoryId | `categoryId=1` | Category filter |
| pageNumber | `pageNumber=1` | Select Page |
| pageSize | `pageSize=10` | Results per Page |
| sortBy | `sortBy=createdAt` | Sorting field |
| sortOrder | `sortOrder=desc` | Ascending / Descending |

Combined Request:

```
GET /api/posts?search=asp.net&categoryId=1&pageNumber=1&pageSize=10&sortBy=createdAt&sortOrder=desc
```

Response:

```
{
  "items": [
    {
      "id": 1,
      "title": "Getting Started with ASP.NET Core",
      "slug": "getting-started-with-asp-net-core",
      "categoryId": 1,
      "categoryName": "ASP.NET Core",
      "createdAt": "2026-11-26T10:00:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 25,
  "totalPages": 3
}
```

**Query & Pagination Rules:**

* Default `pageNumber = 1`, minimum `1`  
* Default `pageSize = 10`, minimum `1`, maximum `100`  
* Allowed `sortBy` values: `createdAt` (default), `title`  
* Allowed `sortOrder` values: `desc` (default), `asc`  
* If any query parameter is invalid (`pageNumber < 1`, `pageSize < 1`, `pageSize > 100`, unknown `sortBy` or `sortOrder`): `400 Bad Request`

### **7.4 Get Post by ID**

GET  
`/api/posts/1`

Success: `200 OK`

If Post is not found: `404 Not Found`

### **7.5 Update Post**

PUT  
`/api/posts/1`

PUT will be used for complete updates.

```
{
  "title": "Updated ASP.NET Core Guide",
  "excerpt": "Updated introduction.",
  "content": "Updated complete article content.",
  "categoryId": 1
}
```

Success: `204 No Content`

### **7.6 Partial Update Post**

PATCH  
`/api/posts/1`

PATCH will receive only the fields intended to be updated.

```
{
  "title": "Advanced ASP.NET Core Guide"
}
```

Success: `204 No Content`

In this project, we will use a custom partial-update DTO (`PatchPostRequest`) backed by an `Optional<T>` wrapper and custom `JsonConverter` rather than a JSON Patch library. Omitted fields in PATCH remain unchanged; explicit `{"excerpt": null}` clears `Excerpt`, while explicit `null` for required fields (`title`, `content`, `categoryId`) returns `400 Bad Request`.

### **7.7 Delete Post**

DELETE  
`/api/posts/1`

* If Post is found: `204 No Content`  
* If Post is not found: `404 Not Found`

## **8\. DTO Design & Validation Rules**

Entities and DTOs are distinct concepts.

* **Entity:** Structure for storing Data in the Database.  
* **DTO:** Structure for exchanging Data with the Client.

We will not return Entities directly as API Responses.

### **8.1 CreatePostRequest**

```
using System.ComponentModel.DataAnnotations;

public record CreatePostRequest(
    [Required]
    [StringLength(200, MinimumLength = 3)]
    string Title,

    [StringLength(500)]
    string? Excerpt,

    [Required]
    [MinLength(10)]
    string Content,

    [Range(1, int.MaxValue)]
    int CategoryId
);
```

### **8.2 UpdatePostRequest**

```
using System.ComponentModel.DataAnnotations;

public record UpdatePostRequest(
    [Required]
    [StringLength(200, MinimumLength = 3)]
    string Title,

    [StringLength(500)]
    string? Excerpt,

    [Required]
    [MinLength(10)]
    string Content,

    [Range(1, int.MaxValue)]
    int CategoryId
);
```

### **8.3 PostResponse & PostSummaryResponse**

```
public record PostResponse(
    int Id,
    string Title,
    string Slug,
    string? Excerpt,
    string Content,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record PostSummaryResponse(
    int Id,
    string Title,
    string Slug,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAt
);
```

`GET /api/posts` returns `PagedResponse<PostSummaryResponse>` so `nvarchar(max)` `Content` is never fetched or serialized in list queries.

### **8.4 PatchPostRequest & PostQueryParameters**

* `PatchPostRequest` wraps `Title`, `Excerpt`, `Content`, and `CategoryId` using `Optional<T>` (`JsonConverter`) to distinguish omitted fields from explicit `null`.
* `PostQueryParameters` validates `PageNumber >= 1`, `1 <= PageSize <= 100`, `SortBy` (`createdAt` | `title`), and `SortOrder` (`asc` | `desc`).

### **8.5 Category DTOs**

```
public record CreateCategoryRequest(
    [Required]
    [StringLength(100, MinimumLength = 2)]
    string Name,

    [StringLength(500)]
    string? Description
);

public record UpdateCategoryRequest(
    [Required]
    [StringLength(100, MinimumLength = 2)]
    string Name,

    [StringLength(500)]
    string? Description
);

public record CategoryResponse(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt
);
```

### **8.6 Validation Rules**

| Field | Rule |
| ----- | ----- |
| Title | Required, 3–200 characters |
| Excerpt | Optional, maximum 500 characters |
| Content | Required, minimum 10 characters |
| CategoryId | Positive integer, existing Category |
| Slug | Server-generated, unique |
| Category Name | Required, 2–100 characters, unique |
| Category Description | Optional, maximum 500 characters |

Using the `[ApiController]` attribute allows ASP.NET Core to automatically respond with `400 Bad Request` on Data Annotation validation failure.

However, verifying whether a `CategoryId` actually exists in the Database must be validated in the Service Layer and return `400 Bad Request` (validation error, per `TC-06`), whereas missing route resources (`/posts/{id}`, `/categories/{id}`) return `404 Not Found`.

## **9\. Business Rules**

Business Rules represent application-specific requirements that must always be enforced.

| ID | Business Rule |
| ----- | ----- |
| BR-01 | Every Post must belong to a valid Category |
| BR-02 | Category Name cannot be duplicate |
| BR-03 | Post Slug must be unique |
| BR-04 | Slug will be generated server-side |
| BR-05 | CreatedAt must be set when a Post is created |
| BR-06 | UpdatedAt must be set when a Post is updated |
| BR-07 | A Category cannot be deleted if it contains Posts |
| BR-08 | Posts cannot be created or updated with an invalid CategoryId |
| BR-09 | Pagination pageNumber cannot be less than 1 |
| BR-10 | PageSize cannot exceed 100 |

**Slug Generation Policy (Locked):** Title text must be normalized to URL-safe lowercase alphanumeric hyphen-separated text (collapsing repeated hyphens and trimming edges; e.g., `"Getting Started with ASP.NET Core"` -> `"getting-started-with-asp-net-core"`, `"C# & .NET 9"` -> `"c-net-9"`).

* **Auto-Suffix on Create (`POST`):** If the generated Slug already exists, the service appends an incremental suffix (`-2`, `-3`, ...) and retries on rare concurrent `DbUpdateException` unique-index collisions so valid posts succeed with `201 Created`.
* **Immutability on Update (`PUT` / `PATCH`):** Updating a Post's `Title` preserves its existing `Slug` so published URLs do not break.
* A Database unique index on `Posts.Slug` ultimately guarantees uniqueness.

## **10\. Error Handling Strategy**

We will use a consistent response format for all API errors.

ASP.NET Core's built-in `ProblemDetails` format will be used.

Example:

```
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Title": [
      "The Title field is required."
    ]
  }
}
```

Proposed Status Codes:

| Status | Meaning | Example |
| ----- | ----- | ----- |
| 200 | OK | GET successful |
| 201 | Created | New Post created |
| 204 | No Content | Update/Delete successful |
| 400 | Bad Request | Validation failure |
| 404 | Not Found | Post does not exist |
| 409 | Conflict | Duplicate Category Name or Category has Posts |
| 500 | Internal Server Error | Unexpected server error |

**Security Rule:** Production responses must never expose SQL queries, Stack Traces, connection strings, or sensitive exception details. Exceptions must be logged server-side.

## **11\. Project Folder Structure**

Our Project Structure will be:

```
BlogManagement/
│
├── BlogManagement.sln
├── docs/
│   ├── BlogDocumentation.md
│   └── screenshots/
├── postman/
│   ├── BlogManagement.postman_collection.json
│   └── BlogManagement.local.postman_environment.json
│
├── src/
│   └── BlogManagement.API/
│       ├── Configurations/
│       │   ├── CategoryConfiguration.cs
│       │   └── PostConfiguration.cs
│       ├── Controllers/
│       │   ├── CategoriesController.cs
│       │   └── PostsController.cs
│       ├── Data/
│       │   ├── BlogDbContext.cs
│       │   └── DbSeeder.cs
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
│       ├── Exceptions/
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
│       ├── Properties/
│       ├── Program.cs
│       ├── appsettings.Development.json
│       ├── appsettings.json
│       └── BlogManagement.API.csproj
│
├── tests/
│   └── BlogManagement.Tests/
│       ├── Helpers/
│       ├── Integration/
│       ├── Services/
│       └── BlogManagement.Tests.csproj
│
├── .gitignore
└── README.md
```

We will create additional folders and files as needed during development beyond the initial starter project structure.

## **12\. NuGet Packages**

| Package | Purpose |
| ----- | ----- |
| Microsoft.EntityFrameworkCore.SqlServer | SQL Server Provider |
| Microsoft.EntityFrameworkCore.Design | EF Core Migration |
| Swashbuckle.AspNetCore | Swagger UI / OpenAPI |
| Microsoft.NET.Test.Sdk | Test Runner |
| xunit | Unit Testing |
| xunit.runner.visualstudio | Visual Studio Test Integration |

All EF Core packages must be kept on the same major version (9.x).

Installation Commands:

```
dotnet add BlogManagement.API package Microsoft.EntityFrameworkCore.SqlServer --version 9.*
dotnet add BlogManagement.API package Microsoft.EntityFrameworkCore.Design --version 9.*
dotnet add BlogManagement.API package Swashbuckle.AspNetCore --version 10.*

dotnet tool install --global dotnet-ef --version 9.*
```

If `dotnet-ef` is already installed:

```
dotnet tool update --global dotnet-ef --version 9.*
```

## **13\. Database Configuration**

Example Connection String for Local SQL Server (`appsettings.Development.json`):

```
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=BlogManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=True;"
  }
}
```

This matches local SQL Server LocalDB on Windows (`MSSQLLocalDB`). Keep `appsettings.json` free of local or production credentials; use `appsettings.Development.json` locally and User Secrets or Environment Variables in production.

### **EF Core Migration Commands**

```
dotnet ef migrations add InitialCreate --project BlogManagement.API

dotnet ef database update --project BlogManagement.API
```

This will generate the Database and Tables in SQL Server.

## **14\. Non-Functional Requirements**

| Category | Requirement |
| ----- | ----- |
| Performance | Select only required columns from the Database |
| Async | Use async/await for Database operations |
| Cancellation | Pass CancellationToken through Controller and Service layers |
| Security | Use parameterized EF Core queries |
| Validation | Validate request data |
| Maintainability | Keep Business Logic out of Controllers |
| Reliability | Consistent Error Response |
| Data Integrity | Primary Key, Foreign Key, Unique Index |
| Scalability | Handle large data sets using Pagination |
| Logging | Log server-side exceptions and necessary events |

### **Performance Considerations**

* Use `AsNoTracking()` for GET operations.  
* Use `Select()` for DTO Projection.  
* Avoid loading all Posts simultaneously without Pagination.  
* Avoid using `Include()` unnecessarily.  
* Execute Search, Sorting, and Pagination directly at the Database query level.  
* Use asynchronous APIs such as `SaveChangesAsync()` and `ToListAsync()`.

## **15\. Testing Strategy**

We will use Postman for API Testing and xUnit for Automated Testing.

### **15.1 Functional Test Cases**

| Test ID | Scenario | Expected Result |
| ----- | ----- | ----- |
| TC-01 | Get all Posts | 200 |
| TC-02 | Get existing Post | 200 |
| TC-03 | Get non-existing Post | 404 |
| TC-04 | Create valid Post | 201 |
| TC-05 | Create Post without Title | 400 |
| TC-06 | Create Post with invalid CategoryId | 400 |
| TC-07 | Create Post with duplicate Title (auto-suffixed Slug) | 201 |
| TC-08 | Update existing Post | 204 |
| TC-09 | Update missing Post | 404 |
| TC-10 | Patch existing Post | 204 |
| TC-11 | Delete existing Post | 204 |
| TC-12 | Delete missing Post | 404 |
| TC-13 | Search Posts | 200 |
| TC-14 | Filter by Category | 200 |
| TC-15 | Pagination | 200 |
| TC-16 | Delete Category containing Posts | 409 |
| TC-17 | Create duplicate Category Name | 409 |

### **15.2 Automated Test Approach**

We will write two types of tests:

* **Unit Tests:** Test Service layer business logic in isolation.  
* **Integration Tests:** Test end-to-end flow from HTTP request down to Database interaction.

SQLite in-memory can be used for Integration Testing. SQLite is better suited than EF Core InMemory provider for validating relational behavior.

## **16\. GitHub Repository & README Documentation**

GitHub Repository Name:

```
blog-management-rest-api
```

Repository Description:

```
A RESTful Blog Management API built with ASP.NET Core 9,
Entity Framework Core 9, and Microsoft SQL Server.
```

README.md will contain the following sections:

1. Project Overview  
2. Features  
3. Technology Stack  
4. Architecture  
5. Database Schema  
6. API Endpoints  
7. Request & Response Examples  
8. Installation Guide  
9. Database Configuration  
10. Migration Instructions  
11. Swagger Documentation  
12. Postman Collection  
13. Testing Instructions  
14. Screenshots  
15. Future Improvements

### **What to Showcase in Fiverr Portfolio**

**Project Portfolio Deliverables**

**Complete Source Code**

**SQL Server Database Schema**

**Swagger / OpenAPI Documentation**

**Postman Collection**

**Automated Test Cases**

**GitHub README**

**Swagger & Postman Screenshots**

Present this as your self-built Practice/Portfolio Project. Do not claim it as a real client project unless it actually is one.

## **17\. Development Roadmap**

The original 14-session draft has been refined into a dependency-ordered **12-Session Roadmap** in `BlogRoadmap.md` (implementing Global `ProblemDetails` Error Handling and Category APIs before Posts, and writing xUnit tests incrementally). Summary:

| Session | Topic | Expected Outcome |
| ----- | ----- | ----- |
| 01 | Spec Lock, Git & Solution Setup | Standalone Solution & Swagger UI |
| 02 | Entities, Fluent Configs & DbContext | `Category`, `Post`, Unique Indexes & `Restrict` FK |
| 03 | Initial Migration & SQL Server | `BlogManagementDb` tables verified |
| 04 | Common Contracts & Global Errors | `PagedResponse` & `ProblemDetails` Middleware |
| 05 | Category DTOs, Service & Unit Tests | Category business logic + Unit Tests |
| 06 | CategoriesController & OpenAPI | Category CRUD endpoints verified |
| 07 | Post DTOs, `Optional<T>` & SlugGenerator | PATCH converter + Slug helper + Tests |
| 08 | PostService CRUD & Unit Tests | Core Post CRUD + Business Rules + Tests |
| 09 | Query Pipeline (Search/Filter/Page/Sort) | DB-level projection & pagination + Tests |
| 10 | PostsController & Swagger Check | Full Posts API verified (`TC-01`..`TC-17`) |
| 11 | Automated Integration Tests | `WebApplicationFactory` + SQLite In-Memory |
| 12 | Postman, Screenshots & GitHub | Portfolio-ready repository & `README.md` |

## **18\. Project Acceptance Criteria**

The project will be considered complete when:

* Application builds and runs successfully.  
* SQL Server Database is created.  
* EF Core Migration works properly.  
* Posts CRUD API works properly.  
* Categories CRUD API works properly.  
* Request Validation works properly.  
* Slug is automatically generated.  
* Duplicate Slugs are handled.  
* Search and Filtering work properly.  
* Pagination and Sorting work properly.  
* Returns correct HTTP Status Codes.  
* Provides consistent Error Responses.  
* Swagger UI contains API Documentation.  
* APIs can be tested using the Postman Collection.  
* Automated Tests pass successfully.  
* GitHub README is complete.

## **19\. Future Enhancements — Version 2.0**

After completing Version 1.0, the following features can be added:

| Feature | Purpose |
| ----- | ----- |
| JWT Authentication | Secure API Access |
| Role-Based Authorization | Admin / Editor / Author |
| User Management | Blog Author Management |
| Image Upload | Post Thumbnail |
| Soft Delete | Data Recovery |
| Comments API | Blog Comments |
| Refresh Token | Token Renewal |
| Rate Limiting | API Abuse Protection |
| Caching | Improve Read Performance |

We will not implement these right away. Our primary goal is to thoroughly build the Core REST API for Version 1.0 first.

## **20\. Final Project Definition**

## **Blog Management REST API — V1.0**

# **Project Specification Defined**

**Project Type:** Backend REST API

**Architecture:** Layered Architecture

**Framework:** ASP.NET Core 9

**Database:** Microsoft SQL Server

**ORM:** Entity Framework Core 9

**API Documentation:** Swagger / OpenAPI

**Testing:** Postman \+ xUnit

**Primary Modules:** Posts \+ Categories

**Development Goal**

Build a maintainable, testable, and production-oriented Blog Management REST API.

**An important note:** This documentation serves as our development specification for V1.0. If any new requirements or technical limitations arise during implementation, we will document the changes rather than silently altering the design.