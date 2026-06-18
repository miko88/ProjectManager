# "Správa projektov firmy" — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a small project-management CRUD web app (Blazor WASM + ASP.NET Core Web API) with login, XML file storage behind a swappable repository port, structured logging, and a test suite — per the design doc `docs/2026-06-18-sprava-projektov-design.md`.

**Architecture:** Clean Architecture (proportionate) + vertical slices. Dependencies point inward: `Domain` ← `Application` (ports) ← `Infrastructure` (XML adapter, auth) and `Api`. `Client` (Blazor WASM) talks to `Api` over HTTP using DTOs in a shared `Contracts` project. Storage is XML behind `IProjectRepository`; auth is self-issued JWT with a config-backed mock user store behind `IUserAuthenticator`.

**Tech Stack:** .NET 10, ASP.NET Core Minimal API, Blazor WebAssembly, FluentValidation, Serilog, JWT bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), `Microsoft.AspNetCore.Identity.PasswordHasher`, `Microsoft.Extensions.Configuration.Xml`, xUnit + FluentAssertions + NSubstitute + `WebApplicationFactory`.

---

## File Structure

```
ProjectManager.sln
├─ src/
│  ├─ ProjectManager.Domain/
│  │  └─ Project.cs                         # entity + invariants (factory + Update)
│  ├─ ProjectManager.Contracts/             # HTTP DTOs shared by Api + Client
│  │  ├─ ProjectDto.cs
│  │  ├─ ProjectRequests.cs                 # Create/Update requests
│  │  └─ AuthContracts.cs                   # LoginRequest / LoginResponse
│  ├─ ProjectManager.Application/
│  │  ├─ Common/Result.cs                   # Result + Result<T> + ResultStatus
│  │  ├─ Abstractions/IProjectRepository.cs
│  │  ├─ Abstractions/IUserAuthenticator.cs # + AuthenticatedUser
│  │  ├─ Abstractions/ITokenService.cs      # + TokenResult
│  │  └─ Features/
│  │     ├─ Projects/
│  │     │  ├─ ListProjects/ListProjectsHandler.cs
│  │     │  ├─ CreateProject/CreateProjectCommand.cs
│  │     │  ├─ CreateProject/CreateProjectValidator.cs
│  │     │  ├─ CreateProject/CreateProjectHandler.cs
│  │     │  ├─ UpdateProject/UpdateProjectCommand.cs
│  │     │  ├─ UpdateProject/UpdateProjectValidator.cs
│  │     │  ├─ UpdateProject/UpdateProjectHandler.cs
│  │     │  └─ DeleteProject/DeleteProjectHandler.cs
│  │     └─ Auth/Login/
│  │        ├─ LoginCommand.cs
│  │        ├─ LoginValidator.cs
│  │        └─ LoginHandler.cs
│  ├─ ProjectManager.Infrastructure/
│  │  ├─ Storage/StorageOptions.cs
│  │  ├─ Storage/XmlProjectRepository.cs    # atomic + serialized writes, encoding-aware
│  │  ├─ Auth/AuthOptions.cs
│  │  ├─ Auth/ConfigUserAuthenticator.cs    # reads user from config, verifies hash
│  │  ├─ Auth/JwtTokenService.cs
│  │  └─ DependencyInjection.cs             # AddInfrastructure(IConfiguration)
│  ├─ ProjectManager.Api/
│  │  ├─ Program.cs                         # Serilog, auth, CORS, DI, exception handler
│  │  ├─ Endpoints/ProjectsEndpoints.cs
│  │  ├─ Endpoints/AuthEndpoints.cs
│  │  ├─ Common/ResultMapping.cs            # Result -> IResult (ProblemDetails)
│  │  ├─ Common/ValidationExtensions.cs     # endpoint filter helper (optional)
│  │  ├─ appsettings.json
│  │  └─ config.xml                         # XML config (storage path, auth, non-secret)
│  └─ ProjectManager.Client/               # Blazor WASM
│     ├─ Program.cs
│     ├─ Services/ProjectApiClient.cs
│     ├─ Services/AuthApiClient.cs
│     ├─ Auth/JwtAuthenticationStateProvider.cs
│     ├─ Auth/BearerTokenHandler.cs
│     ├─ Pages/Login.razor
│     ├─ Pages/Projects.razor
│     ├─ Pages/ProjectEditor.razor          # create/edit dialog component
│     └─ wwwroot/appsettings.json           # ApiBaseUrl
├─ tests/
│  ├─ ProjectManager.Domain.Tests/
│  ├─ ProjectManager.Application.Tests/
│  ├─ ProjectManager.Infrastructure.Tests/
│  └─ ProjectManager.Api.Tests/
├─ data/projects.xml                        # seed (from assignment)
├─ deploy/
│  ├─ docker-compose.yml
│  ├─ api.Dockerfile
│  ├─ client.Dockerfile
│  └─ nginx.conf
└─ README.md
```

---

## Phase 0 — Scaffolding

### Task 0.1: Create solution and projects

**Files:** all project files (generated).

- [ ] **Step 1: Create solution and source projects**

Run from repo root `C:\_projects\ProjectManager`:

```bash
dotnet new sln -n ProjectManager
dotnet new classlib   -n ProjectManager.Domain        -o src/ProjectManager.Domain
dotnet new classlib   -n ProjectManager.Contracts     -o src/ProjectManager.Contracts
dotnet new classlib   -n ProjectManager.Application    -o src/ProjectManager.Application
dotnet new classlib   -n ProjectManager.Infrastructure -o src/ProjectManager.Infrastructure
dotnet new webapi     -n ProjectManager.Api            -o src/ProjectManager.Api --use-minimal-apis
dotnet new blazorwasm -n ProjectManager.Client         -o src/ProjectManager.Client
```

- [ ] **Step 2: Create test projects**

```bash
dotnet new xunit -n ProjectManager.Domain.Tests         -o tests/ProjectManager.Domain.Tests
dotnet new xunit -n ProjectManager.Application.Tests    -o tests/ProjectManager.Application.Tests
dotnet new xunit -n ProjectManager.Infrastructure.Tests -o tests/ProjectManager.Infrastructure.Tests
dotnet new xunit -n ProjectManager.Api.Tests            -o tests/ProjectManager.Api.Tests
```

- [ ] **Step 3: Add all projects to the solution**

```bash
dotnet sln add $(find src tests -name "*.csproj")
```

(On Windows PowerShell, instead run: `Get-ChildItem -Recurse -Filter *.csproj src,tests | ForEach-Object { dotnet sln add $_.FullName }`)

- [ ] **Step 4: Delete template noise**

Remove generated sample files that we will replace: `src/ProjectManager.Domain/Class1.cs`, `src/ProjectManager.Contracts/Class1.cs`, `src/ProjectManager.Application/Class1.cs`, `src/ProjectManager.Infrastructure/Class1.cs`, the weather sample in `src/ProjectManager.Api`, and `UnitTest1.cs` in each test project.

- [ ] **Step 5: Verify it builds**

Run: `dotnet build`
Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "chore: scaffold solution and projects"
```

### Task 0.2: Wire project references

- [ ] **Step 1: Add references (inward-pointing only)**

```bash
dotnet add src/ProjectManager.Application    reference src/ProjectManager.Domain
dotnet add src/ProjectManager.Infrastructure reference src/ProjectManager.Application
dotnet add src/ProjectManager.Api            reference src/ProjectManager.Application src/ProjectManager.Infrastructure src/ProjectManager.Contracts
dotnet add src/ProjectManager.Client         reference src/ProjectManager.Contracts

dotnet add tests/ProjectManager.Domain.Tests         reference src/ProjectManager.Domain
dotnet add tests/ProjectManager.Application.Tests    reference src/ProjectManager.Application
dotnet add tests/ProjectManager.Infrastructure.Tests reference src/ProjectManager.Infrastructure
dotnet add tests/ProjectManager.Api.Tests            reference src/ProjectManager.Api src/ProjectManager.Contracts
```

- [ ] **Step 2: Verify Domain has no outgoing references** — open `src/ProjectManager.Domain/ProjectManager.Domain.csproj` and confirm there are no `<ProjectReference>` or third-party `<PackageReference>` entries. Domain must stay dependency-free.

- [ ] **Step 3: Build and commit**

```bash
dotnet build && git add -A && git commit -m "chore: wire project references (dependencies point inward)"
```

### Task 0.3: Add NuGet packages

- [ ] **Step 1: Add packages per project**

```bash
dotnet add src/ProjectManager.Application    package FluentValidation
dotnet add src/ProjectManager.Infrastructure package Microsoft.AspNetCore.Identity
dotnet add src/ProjectManager.Infrastructure package Microsoft.Extensions.Options.ConfigurationExtensions
dotnet add src/ProjectManager.Infrastructure package System.IdentityModel.Tokens.Jwt
dotnet add src/ProjectManager.Api            package Serilog.AspNetCore
dotnet add src/ProjectManager.Api            package Serilog.Sinks.Console
dotnet add src/ProjectManager.Api            package Serilog.Sinks.File
dotnet add src/ProjectManager.Api            package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/ProjectManager.Api            package Microsoft.Extensions.Configuration.Xml

dotnet add tests/ProjectManager.Application.Tests    package FluentAssertions
dotnet add tests/ProjectManager.Application.Tests    package NSubstitute
dotnet add tests/ProjectManager.Infrastructure.Tests package FluentAssertions
dotnet add tests/ProjectManager.Api.Tests            package FluentAssertions
dotnet add tests/ProjectManager.Api.Tests            package Microsoft.AspNetCore.Mvc.Testing
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build && git add -A && git commit -m "chore: add NuGet packages"
```

### Task 0.4: Seed data and config files

- [ ] **Step 1: Create `data/projects.xml`** with the exact content from the assignment:

```xml
<?xml version="1.0" encoding="windows-1250"?>
<projects>
   <project id="prj1">
      <name>Informačný systém firmy ABC</name>
      <abbreviation>IS-ABC</abbreviation>
      <customer>ABC, s. r. o.</customer>
   </project>
   <project id="prj2">
      <name>Importný modul ISIS</name>
      <abbreviation>Import-ISIS</abbreviation>
      <customer>Homer Simpson</customer>
   </project>
   <project id="prj3">
      <name>Portácia IS-VAK na Oracle</name>
      <abbreviation>OracleVAK</abbreviation>
      <customer>VAK, š. p.</customer>
   </project>
   <project id="prj4">
      <name>Elektronický obchod pre Telecom</name>
      <abbreviation>EComTelecom</abbreviation>
      <customer>Český Telecom, a. s.</customer>
   </project>
   <project id="prj5">
      <name>Rozpoznávanie čiarového kódu pre Delvitu</name>
      <abbreviation>CK-Delvita</abbreviation>
      <customer>Delvita, a. s.</customer>
   </project>
</projects>
```

> Save this file as `windows-1250` encoding to match the declared header (or re-declare as `utf-8` and save UTF-8 — pick one and keep header consistent with bytes). The repository (Task 3.1) reads the declared encoding via `XDocument`/`XmlReader`, which honors the XML declaration.

- [ ] **Step 2: Create `src/ProjectManager.Api/config.xml`** (non-secret config; the XML config provider maps nested elements to `Parent:Child` keys):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <Storage>
    <ProjectsFilePath>../../data/projects.xml</ProjectsFilePath>
  </Storage>
  <Auth>
    <Username>admin</Username>
    <!-- PasswordHash is generated in Task 3.4 Step 1 and pasted here -->
    <PasswordHash>REPLACE_IN_TASK_3.4</PasswordHash>
    <Issuer>ProjectManager</Issuer>
    <Audience>ProjectManagerClient</Audience>
    <TokenExpiryMinutes>60</TokenExpiryMinutes>
    <!-- SigningKey is a SECRET: provided via user-secrets / env, NOT here -->
  </Auth>
</configuration>
```

- [ ] **Step 3: Mark `config.xml` and seed data to copy to output** — in `src/ProjectManager.Api/ProjectManager.Api.csproj` add:

```xml
<ItemGroup>
  <None Update="config.xml" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "chore: add seed projects.xml and config.xml"
```

---

## Phase 1 — Domain

### Task 1.1: `Project` entity with invariants

**Files:**
- Create: `src/ProjectManager.Domain/Project.cs`
- Test: `tests/ProjectManager.Domain.Tests/ProjectTests.cs`

- [ ] **Step 1: Write the failing tests**

`tests/ProjectManager.Domain.Tests/ProjectTests.cs`:

```csharp
using FluentAssertions;
using ProjectManager.Domain;
using Xunit;

namespace ProjectManager.Domain.Tests;

public class ProjectTests
{
    [Fact]
    public void Create_WithValidValues_TrimsAndAssigns()
    {
        var p = Project.Create("prj1", "  Name  ", " ABC ", " Cust ");

        p.Id.Should().Be("prj1");
        p.Name.Should().Be("Name");
        p.Abbreviation.Should().Be("ABC");
        p.Customer.Should().Be("Cust");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankName_Throws(string? name)
    {
        var act = () => Project.Create("prj1", name!, "ABC", "Cust");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithBlankId_Throws()
    {
        var act = () => Project.Create("  ", "Name", "ABC", "Cust");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ChangesMutableFields_KeepsId()
    {
        var p = Project.Create("prj1", "Old", "OLD", "OldCust");
        p.Update("New", "NEW", "NewCust");

        p.Id.Should().Be("prj1");
        p.Name.Should().Be("New");
        p.Abbreviation.Should().Be("NEW");
        p.Customer.Should().Be("NewCust");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ProjectManager.Domain.Tests`
Expected: FAIL (type `Project` does not exist).

- [ ] **Step 3: Implement `Project`**

`src/ProjectManager.Domain/Project.cs`:

```csharp
namespace ProjectManager.Domain;

/// <summary>
/// A company project. Cannot be constructed in an invalid state — all mutation
/// goes through the factory/Update which enforce invariants. This is the last-line
/// backstop; primary input validation happens in the Application layer.
/// </summary>
public sealed class Project
{
    public string Id { get; }
    public string Name { get; private set; }
    public string Abbreviation { get; private set; }
    public string Customer { get; private set; }

    private Project(string id, string name, string abbreviation, string customer)
    {
        Id = id;
        Name = name;
        Abbreviation = abbreviation;
        Customer = customer;
    }

    public static Project Create(string id, string name, string abbreviation, string customer)
    {
        Require(id, nameof(id));
        Require(name, nameof(name));
        Require(abbreviation, nameof(abbreviation));
        Require(customer, nameof(customer));

        return new Project(id.Trim(), name.Trim(), abbreviation.Trim(), customer.Trim());
    }

    public void Update(string name, string abbreviation, string customer)
    {
        Require(name, nameof(name));
        Require(abbreviation, nameof(abbreviation));
        Require(customer, nameof(customer));

        Name = name.Trim();
        Abbreviation = abbreviation.Trim();
        Customer = customer.Trim();
    }

    private static void Require(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"'{field}' must not be empty.", field);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ProjectManager.Domain.Tests`
Expected: PASS (5 passing including theory cases).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(domain): Project entity with invariants"
```

---

## Phase 2 — Application

### Task 2.1: `Result` and `Result<T>`

**Files:**
- Create: `src/ProjectManager.Application/Common/Result.cs`
- Test: `tests/ProjectManager.Application.Tests/Common/ResultTests.cs`

- [ ] **Step 1: Write failing tests**

`tests/ProjectManager.Application.Tests/Common/ResultTests.cs`:

```csharp
using FluentAssertions;
using ProjectManager.Application.Common;
using Xunit;

namespace ProjectManager.Application.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_HasSuccessStatus()
    {
        var r = Result.Success();
        r.IsSuccess.Should().BeTrue();
        r.Status.Should().Be(ResultStatus.Success);
    }

    [Fact]
    public void NotFound_CarriesMessageAndIsNotSuccess()
    {
        var r = Result.NotFound("missing");
        r.IsSuccess.Should().BeFalse();
        r.Status.Should().Be(ResultStatus.NotFound);
        r.Message.Should().Be("missing");
    }

    [Fact]
    public void Invalid_CarriesValidationErrors()
    {
        var errors = new Dictionary<string, string[]> { ["Name"] = new[] { "Required" } };
        var r = Result.Invalid(errors);
        r.Status.Should().Be(ResultStatus.Invalid);
        r.ValidationErrors.Should().ContainKey("Name");
    }

    [Fact]
    public void GenericSuccess_CarriesValue()
    {
        var r = Result<int>.Success(42);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(42);
    }

    [Fact]
    public void GenericNotFound_HasNoValue()
    {
        var r = Result<int>.NotFound("x");
        r.IsSuccess.Should().BeFalse();
        r.Status.Should().Be(ResultStatus.NotFound);
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/ProjectManager.Application.Tests`
Expected: FAIL (Result not defined).

- [ ] **Step 3: Implement Result types**

`src/ProjectManager.Application/Common/Result.cs`:

```csharp
namespace ProjectManager.Application.Common;

public enum ResultStatus
{
    Success,
    Invalid,
    NotFound,
    Conflict,
    Unauthorized
}

/// <summary>
/// Explicit outcome of an application operation. Expected failures are modeled here;
/// exceptions are reserved for genuinely unexpected conditions.
/// </summary>
public class Result
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors =
        new Dictionary<string, string[]>();

    public ResultStatus Status { get; }
    public string? Message { get; }
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    public bool IsSuccess => Status == ResultStatus.Success;

    protected Result(ResultStatus status, string? message, IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Status = status;
        Message = message;
        ValidationErrors = validationErrors ?? NoErrors;
    }

    public static Result Success() => new(ResultStatus.Success, null, null);
    public static Result NotFound(string message) => new(ResultStatus.NotFound, message, null);
    public static Result Conflict(string message) => new(ResultStatus.Conflict, message, null);
    public static Result Unauthorized(string message) => new(ResultStatus.Unauthorized, message, null);
    public static Result Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new(ResultStatus.Invalid, null, errors);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(ResultStatus.Success, null, null) => Value = value;
    private Result(ResultStatus status, string? message, IReadOnlyDictionary<string, string[]>? errors)
        : base(status, message, errors) => Value = default;

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> NotFound(string message) => new(ResultStatus.NotFound, message, null);
    public static new Result<T> Conflict(string message) => new(ResultStatus.Conflict, message, null);
    public static new Result<T> Unauthorized(string message) => new(ResultStatus.Unauthorized, message, null);
    public static new Result<T> Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new(ResultStatus.Invalid, null, errors);
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/ProjectManager.Application.Tests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(application): Result and Result<T> types"
```

### Task 2.2: Ports (abstractions)

**Files:**
- Create: `src/ProjectManager.Application/Abstractions/IProjectRepository.cs`
- Create: `src/ProjectManager.Application/Abstractions/IUserAuthenticator.cs`
- Create: `src/ProjectManager.Application/Abstractions/ITokenService.cs`

No tests (interfaces only — exercised by handler/infra tests).

- [ ] **Step 1: Create `IProjectRepository.cs`**

```csharp
using ProjectManager.Domain;

namespace ProjectManager.Application.Abstractions;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default);
    Task<Project?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
    Task UpdateAsync(Project project, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<string> NextIdAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Create `IUserAuthenticator.cs`**

```csharp
using ProjectManager.Application.Common;

namespace ProjectManager.Application.Abstractions;

public sealed record AuthenticatedUser(string Username);

public interface IUserAuthenticator
{
    Task<Result<AuthenticatedUser>> AuthenticateAsync(string username, string password, CancellationToken ct = default);
}
```

- [ ] **Step 3: Create `ITokenService.cs`**

```csharp
using ProjectManager.Application.Abstractions;

namespace ProjectManager.Application.Abstractions;

public sealed record TokenResult(string Token, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    TokenResult CreateToken(AuthenticatedUser user);
}
```

- [ ] **Step 4: Build and commit**

```bash
dotnet build && git add -A && git commit -m "feat(application): repository, authenticator, token ports"
```

### Task 2.3: ListProjects handler

**Files:**
- Create: `src/ProjectManager.Application/Features/Projects/ListProjects/ListProjectsHandler.cs`
- Test: `tests/ProjectManager.Application.Tests/Features/Projects/ListProjectsHandlerTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using FluentAssertions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Features.Projects.ListProjects;
using ProjectManager.Domain;
using Xunit;

namespace ProjectManager.Application.Tests.Features.Projects;

public class ListProjectsHandlerTests
{
    [Fact]
    public async Task ReturnsAllProjectsFromRepository()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Project.Create("prj1", "A", "A", "C") });

        var handler = new ListProjectsHandler(repo);
        var result = await handler.HandleAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/ProjectManager.Application.Tests`
Expected: FAIL (ListProjectsHandler not defined).

- [ ] **Step 3: Implement handler**

```csharp
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Domain;

namespace ProjectManager.Application.Features.Projects.ListProjects;

public sealed class ListProjectsHandler(IProjectRepository repository)
{
    public async Task<Result<IReadOnlyList<Project>>> HandleAsync(CancellationToken ct = default)
    {
        var projects = await repository.GetAllAsync(ct);
        return Result<IReadOnlyList<Project>>.Success(projects);
    }
}
```

- [ ] **Step 4: Run to verify pass** — `dotnet test tests/ProjectManager.Application.Tests` → PASS.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(application): ListProjects handler"
```

### Task 2.4: CreateProject command, validator, handler

**Files:**
- Create: `src/ProjectManager.Application/Features/Projects/CreateProject/CreateProjectCommand.cs`
- Create: `.../CreateProject/CreateProjectValidator.cs`
- Create: `.../CreateProject/CreateProjectHandler.cs`
- Test: `tests/ProjectManager.Application.Tests/Features/Projects/CreateProjectHandlerTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using FluentAssertions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Projects.CreateProject;
using ProjectManager.Domain;
using Xunit;

namespace ProjectManager.Application.Tests.Features.Projects;

public class CreateProjectHandlerTests
{
    private static CreateProjectHandler Build(IProjectRepository repo) =>
        new(repo, new CreateProjectValidator());

    [Fact]
    public async Task ValidCommand_AddsProjectWithGeneratedId()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Project>());
        repo.NextIdAsync(Arg.Any<CancellationToken>()).Returns("prj6");

        var result = await Build(repo).HandleAsync(new CreateProjectCommand("Name", "ABBR", "Cust"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("prj6");
        await repo.Received(1).AddAsync(Arg.Is<Project>(p => p.Id == "prj6"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BlankName_ReturnsInvalid_AndDoesNotPersist()
    {
        var repo = Substitute.For<IProjectRepository>();

        var result = await Build(repo).HandleAsync(new CreateProjectCommand("", "ABBR", "Cust"));

        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().ContainKey(nameof(CreateProjectCommand.Name));
        await repo.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateAbbreviation_ReturnsConflict()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Project.Create("prj1", "X", "ABBR", "C") });

        var result = await Build(repo).HandleAsync(new CreateProjectCommand("Name", "ABBR", "Cust"));

        result.Status.Should().Be(ResultStatus.Conflict);
    }
}
```

- [ ] **Step 2: Run to verify fail** — `dotnet test tests/ProjectManager.Application.Tests` → FAIL.

- [ ] **Step 3: Implement command**

`CreateProjectCommand.cs`:

```csharp
namespace ProjectManager.Application.Features.Projects.CreateProject;

public sealed record CreateProjectCommand(string Name, string Abbreviation, string Customer);
```

- [ ] **Step 4: Implement validator**

`CreateProjectValidator.cs`:

```csharp
using FluentValidation;

namespace ProjectManager.Application.Features.Projects.CreateProject;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Abbreviation).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Customer).NotEmpty().MaximumLength(200);
    }
}
```

- [ ] **Step 5: Implement handler**

`CreateProjectHandler.cs`:

```csharp
using FluentValidation;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Domain;

namespace ProjectManager.Application.Features.Projects.CreateProject;

public sealed class CreateProjectHandler(
    IProjectRepository repository,
    IValidator<CreateProjectCommand> validator)
{
    public async Task<Result<Project>> HandleAsync(CreateProjectCommand command, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<Project>.Invalid(validation.ToErrorDictionary());

        var existing = await repository.GetAllAsync(ct);
        if (existing.Any(p => string.Equals(p.Abbreviation, command.Abbreviation.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Result<Project>.Conflict($"A project with abbreviation '{command.Abbreviation}' already exists.");

        var id = await repository.NextIdAsync(ct);
        var project = Project.Create(id, command.Name, command.Abbreviation, command.Customer);
        await repository.AddAsync(project, ct);

        return Result<Project>.Success(project);
    }
}
```

- [ ] **Step 6: Add the `ToErrorDictionary` helper**

Create `src/ProjectManager.Application/Common/ValidationResultExtensions.cs`:

```csharp
using FluentValidation.Results;

namespace ProjectManager.Application.Common;

public static class ValidationResultExtensions
{
    public static IReadOnlyDictionary<string, string[]> ToErrorDictionary(this ValidationResult result) =>
        result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
```

- [ ] **Step 7: Run to verify pass** — `dotnet test tests/ProjectManager.Application.Tests` → PASS.

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feat(application): CreateProject command/validator/handler"
```

### Task 2.5: UpdateProject command, validator, handler

**Files:**
- Create: `.../UpdateProject/UpdateProjectCommand.cs`, `UpdateProjectValidator.cs`, `UpdateProjectHandler.cs`
- Test: `tests/ProjectManager.Application.Tests/Features/Projects/UpdateProjectHandlerTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using FluentAssertions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Projects.UpdateProject;
using ProjectManager.Domain;
using Xunit;

namespace ProjectManager.Application.Tests.Features.Projects;

public class UpdateProjectHandlerTests
{
    private static UpdateProjectHandler Build(IProjectRepository repo) =>
        new(repo, new UpdateProjectValidator());

    [Fact]
    public async Task ExistingProject_IsUpdatedAndPersisted()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("prj1", Arg.Any<CancellationToken>())
            .Returns(Project.Create("prj1", "Old", "OLD", "OldC"));
        repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Project.Create("prj1", "Old", "OLD", "OldC") });

        var result = await Build(repo).HandleAsync(new UpdateProjectCommand("prj1", "New", "NEW", "NewC"));

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).UpdateAsync(Arg.Is<Project>(p => p.Name == "New"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingProject_ReturnsNotFound()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("nope", Arg.Any<CancellationToken>()).Returns((Project?)null);

        var result = await Build(repo).HandleAsync(new UpdateProjectCommand("nope", "New", "NEW", "NewC"));

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task BlankName_ReturnsInvalid()
    {
        var repo = Substitute.For<IProjectRepository>();
        var result = await Build(repo).HandleAsync(new UpdateProjectCommand("prj1", "", "NEW", "NewC"));
        result.Status.Should().Be(ResultStatus.Invalid);
    }
}
```

- [ ] **Step 2: Run to verify fail** — `dotnet test tests/ProjectManager.Application.Tests` → FAIL.

- [ ] **Step 3: Implement command**

```csharp
namespace ProjectManager.Application.Features.Projects.UpdateProject;

public sealed record UpdateProjectCommand(string Id, string Name, string Abbreviation, string Customer);
```

- [ ] **Step 4: Implement validator**

```csharp
using FluentValidation;

namespace ProjectManager.Application.Features.Projects.UpdateProject;

public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Abbreviation).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Customer).NotEmpty().MaximumLength(200);
    }
}
```

- [ ] **Step 5: Implement handler**

```csharp
using FluentValidation;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;

namespace ProjectManager.Application.Features.Projects.UpdateProject;

public sealed class UpdateProjectHandler(
    IProjectRepository repository,
    IValidator<UpdateProjectCommand> validator)
{
    public async Task<Result> HandleAsync(UpdateProjectCommand command, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result.Invalid(validation.ToErrorDictionary());

        var project = await repository.GetByIdAsync(command.Id, ct);
        if (project is null)
            return Result.NotFound($"Project '{command.Id}' was not found.");

        var others = await repository.GetAllAsync(ct);
        if (others.Any(p => p.Id != command.Id &&
                            string.Equals(p.Abbreviation, command.Abbreviation.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Result.Conflict($"A project with abbreviation '{command.Abbreviation}' already exists.");

        project.Update(command.Name, command.Abbreviation, command.Customer);
        await repository.UpdateAsync(project, ct);

        return Result.Success();
    }
}
```

- [ ] **Step 6: Run to verify pass** — `dotnet test tests/ProjectManager.Application.Tests` → PASS.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat(application): UpdateProject command/validator/handler"
```

### Task 2.6: DeleteProject handler

**Files:**
- Create: `.../DeleteProject/DeleteProjectHandler.cs`
- Test: `tests/ProjectManager.Application.Tests/Features/Projects/DeleteProjectHandlerTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using FluentAssertions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Projects.DeleteProject;
using ProjectManager.Domain;
using Xunit;

namespace ProjectManager.Application.Tests.Features.Projects;

public class DeleteProjectHandlerTests
{
    [Fact]
    public async Task ExistingProject_IsDeleted()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("prj1", Arg.Any<CancellationToken>())
            .Returns(Project.Create("prj1", "A", "A", "C"));

        var result = await new DeleteProjectHandler(repo).HandleAsync("prj1");

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).DeleteAsync("prj1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingProject_ReturnsNotFound()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("nope", Arg.Any<CancellationToken>()).Returns((Project?)null);

        var result = await new DeleteProjectHandler(repo).HandleAsync("nope");

        result.Status.Should().Be(ResultStatus.NotFound);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Run to verify fail** — FAIL.

- [ ] **Step 3: Implement handler**

```csharp
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;

namespace ProjectManager.Application.Features.Projects.DeleteProject;

public sealed class DeleteProjectHandler(IProjectRepository repository)
{
    public async Task<Result> HandleAsync(string id, CancellationToken ct = default)
    {
        var project = await repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result.NotFound($"Project '{id}' was not found.");

        await repository.DeleteAsync(id, ct);
        return Result.Success();
    }
}
```

- [ ] **Step 4: Run to verify pass** — PASS.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(application): DeleteProject handler"
```

### Task 2.7: Login command, validator, handler

**Files:**
- Create: `.../Auth/Login/LoginCommand.cs`, `LoginValidator.cs`, `LoginHandler.cs`
- Test: `tests/ProjectManager.Application.Tests/Features/Auth/LoginHandlerTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using FluentAssertions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Auth.Login;
using Xunit;

namespace ProjectManager.Application.Tests.Features.Auth;

public class LoginHandlerTests
{
    private static LoginHandler Build(IUserAuthenticator auth, ITokenService tokens) =>
        new(auth, tokens, new LoginValidator());

    [Fact]
    public async Task ValidCredentials_ReturnsToken()
    {
        var auth = Substitute.For<IUserAuthenticator>();
        auth.AuthenticateAsync("admin", "pw", Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedUser>.Success(new AuthenticatedUser("admin")));
        var tokens = Substitute.For<ITokenService>();
        tokens.CreateToken(Arg.Any<AuthenticatedUser>())
            .Returns(new TokenResult("jwt-123", DateTimeOffset.UtcNow.AddHours(1)));

        var result = await Build(auth, tokens).HandleAsync(new LoginCommand("admin", "pw"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("jwt-123");
    }

    [Fact]
    public async Task BadCredentials_ReturnsUnauthorized_AndNoToken()
    {
        var auth = Substitute.For<IUserAuthenticator>();
        auth.AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedUser>.Unauthorized("bad"));
        var tokens = Substitute.For<ITokenService>();

        var result = await Build(auth, tokens).HandleAsync(new LoginCommand("admin", "wrong"));

        result.Status.Should().Be(ResultStatus.Unauthorized);
        tokens.DidNotReceive().CreateToken(Arg.Any<AuthenticatedUser>());
    }

    [Fact]
    public async Task BlankUsername_ReturnsInvalid()
    {
        var result = await Build(Substitute.For<IUserAuthenticator>(), Substitute.For<ITokenService>())
            .HandleAsync(new LoginCommand("", "pw"));
        result.Status.Should().Be(ResultStatus.Invalid);
    }
}
```

- [ ] **Step 2: Run to verify fail** — FAIL.

- [ ] **Step 3: Implement command + validator**

`LoginCommand.cs`:

```csharp
namespace ProjectManager.Application.Features.Auth.Login;

public sealed record LoginCommand(string Username, string Password);
```

`LoginValidator.cs`:

```csharp
using FluentValidation;

namespace ProjectManager.Application.Features.Auth.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
```

- [ ] **Step 4: Implement handler**

`LoginHandler.cs`:

```csharp
using FluentValidation;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;

namespace ProjectManager.Application.Features.Auth.Login;

public sealed class LoginHandler(
    IUserAuthenticator authenticator,
    ITokenService tokenService,
    IValidator<LoginCommand> validator)
{
    public async Task<Result<TokenResult>> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<TokenResult>.Invalid(validation.ToErrorDictionary());

        var auth = await authenticator.AuthenticateAsync(command.Username, command.Password, ct);
        if (!auth.IsSuccess)
            return Result<TokenResult>.Unauthorized("Invalid username or password.");

        var token = tokenService.CreateToken(auth.Value!);
        return Result<TokenResult>.Success(token);
    }
}
```

> Note: on bad credentials we return a generic message (no hint whether username or password was wrong) — a small but real security detail.

- [ ] **Step 5: Run to verify pass** — PASS.

- [ ] **Step 6: Register Application services for DI**

Create `src/ProjectManager.Application/DependencyInjection.cs`:

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProjectManager.Application.Features.Auth.Login;
using ProjectManager.Application.Features.Projects.CreateProject;
using ProjectManager.Application.Features.Projects.DeleteProject;
using ProjectManager.Application.Features.Projects.ListProjects;
using ProjectManager.Application.Features.Projects.UpdateProject;

namespace ProjectManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateProjectValidator>();

        services.AddScoped<ListProjectsHandler>();
        services.AddScoped<CreateProjectHandler>();
        services.AddScoped<UpdateProjectHandler>();
        services.AddScoped<DeleteProjectHandler>();
        services.AddScoped<LoginHandler>();

        return services;
    }
}
```

Add package: `dotnet add src/ProjectManager.Application package FluentValidation.DependencyInjectionExtensions` and `dotnet add src/ProjectManager.Application package Microsoft.Extensions.DependencyInjection.Abstractions`.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat(application): Login handler + DI registration"
```

---

## Phase 3 — Infrastructure

### Task 3.1: `XmlProjectRepository` (atomic, serialized, encoding-aware)

**Files:**
- Create: `src/ProjectManager.Infrastructure/Storage/StorageOptions.cs`
- Create: `src/ProjectManager.Infrastructure/Storage/XmlProjectRepository.cs`
- Test: `tests/ProjectManager.Infrastructure.Tests/XmlProjectRepositoryTests.cs`

- [ ] **Step 1: Write failing tests (against real temp files)**

```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProjectManager.Domain;
using ProjectManager.Infrastructure.Storage;
using Xunit;

namespace ProjectManager.Infrastructure.Tests;

public class XmlProjectRepositoryTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"pm-{Guid.NewGuid():N}.xml");

    private XmlProjectRepository Build()
    {
        var options = Options.Create(new StorageOptions { ProjectsFilePath = _file });
        return new XmlProjectRepository(options, NullLogger<XmlProjectRepository>.Instance);
    }

    private async Task SeedAsync(string xml) => await File.WriteAllTextAsync(_file, xml);

    [Fact]
    public async Task GetAll_ReadsSeededProjects()
    {
        await SeedAsync("""
            <?xml version="1.0" encoding="utf-8"?>
            <projects>
              <project id="prj1"><name>A</name><abbreviation>A1</abbreviation><customer>C</customer></project>
            </projects>
            """);

        var all = await Build().GetAllAsync();

        all.Should().HaveCount(1);
        all[0].Id.Should().Be("prj1");
        all[0].Abbreviation.Should().Be("A1");
    }

    [Fact]
    public async Task MissingFile_ReturnsEmpty()
    {
        var all = await Build().GetAllAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task CorruptXml_ThrowsInvalidDataException()
    {
        await SeedAsync("<projects><project></broken>");
        var act = async () => await Build().GetAllAsync();
        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task Add_ThenGetById_RoundTrips()
    {
        var repo = Build();
        await repo.AddAsync(Project.Create("prj9", "New", "NEW", "Cust"));

        var loaded = await repo.GetByIdAsync("prj9");
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("New");
    }

    [Fact]
    public async Task NextId_IsMaxPlusOne()
    {
        await SeedAsync("""
            <projects>
              <project id="prj3"><name>A</name><abbreviation>A</abbreviation><customer>C</customer></project>
              <project id="prj7"><name>B</name><abbreviation>B</abbreviation><customer>C</customer></project>
            </projects>
            """);

        var next = await Build().NextIdAsync();
        next.Should().Be("prj8");
    }

    [Fact]
    public async Task ConcurrentAdds_DoNotCorruptFile_AllPersist()
    {
        var repo = Build();
        await repo.AddAsync(Project.Create("seed", "S", "S", "C"));

        var tasks = Enumerable.Range(0, 20).Select(i =>
            repo.AddAsync(Project.Create($"p{i}", $"N{i}", $"AB{i}", "C")));
        await Task.WhenAll(tasks);

        var all = await repo.GetAllAsync();
        all.Should().HaveCount(21);
    }

    public void Dispose()
    {
        if (File.Exists(_file)) File.Delete(_file);
    }
}
```

- [ ] **Step 2: Run to verify fail** — `dotnet test tests/ProjectManager.Infrastructure.Tests` → FAIL.

- [ ] **Step 3: Implement `StorageOptions`**

```csharp
namespace ProjectManager.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public string ProjectsFilePath { get; set; } = "data/projects.xml";
}
```

- [ ] **Step 4: Implement `XmlProjectRepository`**

```csharp
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectManager.Application.Abstractions;
using ProjectManager.Domain;

namespace ProjectManager.Infrastructure.Storage;

/// <summary>
/// XML-file implementation of <see cref="IProjectRepository"/>.
/// Reads through to disk on every call (small file, no stale-cache class of bugs).
/// Writes are serialized via a semaphore and committed atomically (temp file + replace)
/// so the store is never left half-written, even under concurrency.
/// </summary>
public sealed class XmlProjectRepository : IProjectRepository
{
    private readonly string _path;
    private readonly ILogger<XmlProjectRepository> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public XmlProjectRepository(IOptions<StorageOptions> options, ILogger<XmlProjectRepository> logger)
    {
        _path = options.Value.ProjectsFilePath;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
        {
            _logger.LogWarning("Projects file {Path} does not exist; returning empty list.", _path);
            return Array.Empty<Project>();
        }

        XDocument doc;
        try
        {
            await using var stream = File.OpenRead(_path);
            doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct);
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "Projects file {Path} is not valid XML.", _path);
            throw new InvalidDataException($"Projects file '{_path}' is corrupt.", ex);
        }

        return doc.Root?.Elements("project").Select(ToProject).ToList() ?? new List<Project>();
    }

    public async Task<Project?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.FirstOrDefault(p => p.Id == id);
    }

    public async Task<string> NextIdAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        var max = all
            .Select(p => int.TryParse(p.Id.Replace("prj", ""), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"prj{max + 1}";
    }

    public Task AddAsync(Project project, CancellationToken ct = default) =>
        MutateAsync(list =>
        {
            if (list.Any(p => p.Id == project.Id))
                throw new InvalidOperationException($"Project '{project.Id}' already exists.");
            list.Add(project);
        }, ct);

    public Task UpdateAsync(Project project, CancellationToken ct = default) =>
        MutateAsync(list =>
        {
            var idx = list.FindIndex(p => p.Id == project.Id);
            if (idx < 0) throw new InvalidOperationException($"Project '{project.Id}' not found.");
            list[idx] = project;
        }, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        MutateAsync(list => list.RemoveAll(p => p.Id == id), ct);

    private async Task MutateAsync(Action<List<Project>> mutate, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            var list = (await GetAllAsync(ct)).ToList();
            mutate(list);
            await SaveAtomicAsync(list, ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task SaveAtomicAsync(IReadOnlyList<Project> projects, CancellationToken ct)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("projects",
                projects.Select(p => new XElement("project",
                    new XAttribute("id", p.Id),
                    new XElement("name", p.Name),
                    new XElement("abbreviation", p.Abbreviation),
                    new XElement("customer", p.Customer)))));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);

        var tmp = _path + ".tmp";
        await using (var stream = File.Create(tmp))
        {
            await doc.SaveAsync(stream, SaveOptions.None, ct);
        }

        // Atomic replace: the store is never observed half-written.
        if (File.Exists(_path))
            File.Replace(tmp, _path, null);
        else
            File.Move(tmp, _path);
    }

    private static Project ToProject(XElement e) => Project.Create(
        (string?)e.Attribute("id") ?? throw new InvalidDataException("project@id is missing."),
        (string?)e.Element("name") ?? "",
        (string?)e.Element("abbreviation") ?? "",
        (string?)e.Element("customer") ?? "");
}
```

- [ ] **Step 5: Run to verify pass** — `dotnet test tests/ProjectManager.Infrastructure.Tests` → PASS (all, including concurrency test).

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(infra): XmlProjectRepository with atomic, serialized writes"
```

### Task 3.2: Auth options + password hashing + `ConfigUserAuthenticator`

**Files:**
- Create: `src/ProjectManager.Infrastructure/Auth/AuthOptions.cs`
- Create: `src/ProjectManager.Infrastructure/Auth/ConfigUserAuthenticator.cs`
- Test: `tests/ProjectManager.Infrastructure.Tests/ConfigUserAuthenticatorTests.cs`

- [ ] **Step 1: Implement `AuthOptions`**

```csharp
namespace ProjectManager.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Issuer { get; set; } = "ProjectManager";
    public string Audience { get; set; } = "ProjectManagerClient";
    public int TokenExpiryMinutes { get; set; } = 60;
    public string SigningKey { get; set; } = "";
}
```

- [ ] **Step 2: Write failing tests**

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProjectManager.Application.Common;
using ProjectManager.Infrastructure.Auth;
using Xunit;

namespace ProjectManager.Infrastructure.Tests;

public class ConfigUserAuthenticatorTests
{
    private static (ConfigUserAuthenticator auth, string password) Build()
    {
        var hasher = new PasswordHasher<string>();
        var hash = hasher.HashPassword("admin", "Secret123!");
        var options = Options.Create(new AuthOptions { Username = "admin", PasswordHash = hash });
        return (new ConfigUserAuthenticator(options), "Secret123!");
    }

    [Fact]
    public async Task CorrectCredentials_ReturnsSuccess()
    {
        var (auth, password) = Build();
        var result = await auth.AuthenticateAsync("admin", password);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Username.Should().Be("admin");
    }

    [Fact]
    public async Task WrongPassword_ReturnsUnauthorized()
    {
        var (auth, _) = Build();
        var result = await auth.AuthenticateAsync("admin", "wrong");
        result.Status.Should().Be(ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task UnknownUser_ReturnsUnauthorized()
    {
        var (auth, password) = Build();
        var result = await auth.AuthenticateAsync("intruder", password);
        result.Status.Should().Be(ResultStatus.Unauthorized);
    }
}
```

- [ ] **Step 3: Run to verify fail** — FAIL.

- [ ] **Step 4: Implement `ConfigUserAuthenticator`**

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;

namespace ProjectManager.Infrastructure.Auth;

/// <summary>
/// Mock user store: a single user read from configuration, password verified against a
/// stored PBKDF2 hash (never plaintext). Behind <see cref="IUserAuthenticator"/> so it can
/// be swapped for a real identity provider without touching the application layer.
/// </summary>
public sealed class ConfigUserAuthenticator(IOptions<AuthOptions> options) : IUserAuthenticator
{
    private readonly AuthOptions _options = options.Value;
    private readonly PasswordHasher<string> _hasher = new();

    public Task<Result<AuthenticatedUser>> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        if (!string.Equals(username, _options.Username, StringComparison.Ordinal))
            return Task.FromResult(Result<AuthenticatedUser>.Unauthorized("Invalid credentials."));

        var verify = _hasher.VerifyHashedPassword(username, _options.PasswordHash, password);
        if (verify == PasswordVerificationResult.Failed)
            return Task.FromResult(Result<AuthenticatedUser>.Unauthorized("Invalid credentials."));

        return Task.FromResult(Result<AuthenticatedUser>.Success(new AuthenticatedUser(username)));
    }
}
```

- [ ] **Step 5: Run to verify pass** — PASS.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(infra): config-backed user authenticator with hashed password"
```

### Task 3.3: `JwtTokenService`

**Files:**
- Create: `src/ProjectManager.Infrastructure/Auth/JwtTokenService.cs`
- Test: `tests/ProjectManager.Infrastructure.Tests/JwtTokenServiceTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using ProjectManager.Application.Abstractions;
using ProjectManager.Infrastructure.Auth;
using Xunit;

namespace ProjectManager.Infrastructure.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_ProducesValidJwt_WithExpectedClaims()
    {
        var options = Options.Create(new AuthOptions
        {
            Issuer = "ProjectManager",
            Audience = "ProjectManagerClient",
            TokenExpiryMinutes = 30,
            SigningKey = "this-is-a-long-enough-test-signing-key-0123456789"
        });
        var service = new JwtTokenService(options);

        var result = service.CreateToken(new AuthenticatedUser("admin"));

        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Issuer.Should().Be("ProjectManager");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "admin");
    }
}
```

- [ ] **Step 2: Run to verify fail** — FAIL.

- [ ] **Step 3: Implement `JwtTokenService`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectManager.Application.Abstractions;

namespace ProjectManager.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<AuthOptions> options) : ITokenService
{
    private readonly AuthOptions _options = options.Value;

    public TokenResult CreateToken(AuthenticatedUser user)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.TokenExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenResult(encoded, expires);
    }
}
```

- [ ] **Step 4: Run to verify pass** — PASS.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(infra): JWT token service"
```

### Task 3.4: Infrastructure DI + generate password hash

**Files:**
- Create: `src/ProjectManager.Infrastructure/DependencyInjection.cs`
- Modify: `src/ProjectManager.Api/config.xml` (paste real hash)

- [ ] **Step 1: Generate the password hash for the seed user**

Add a temporary fact in `tests/ProjectManager.Infrastructure.Tests` (or run via `dotnet fsi`/a scratch console) to print the hash, then delete it:

```csharp
[Fact(Skip = "dev utility — un-skip to print a hash, then re-skip")]
public void PrintHash()
{
    var hash = new Microsoft.AspNetCore.Identity.PasswordHasher<string>()
        .HashPassword("admin", "Admin123!");
    System.Console.WriteLine(hash);
    Assert.True(true);
}
```

Run un-skipped with `dotnet test --logger "console;verbosity=detailed"`, copy the printed hash into `config.xml` `<PasswordHash>`. Re-skip the test (or delete it). Document the dev password (`admin` / `Admin123!`) in the README.

- [ ] **Step 2: Implement Infrastructure DI**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManager.Application.Abstractions;
using ProjectManager.Infrastructure.Auth;
using ProjectManager.Infrastructure.Storage;

namespace ProjectManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<StorageOptions>(config.GetSection(StorageOptions.SectionName));
        services.Configure<AuthOptions>(config.GetSection(AuthOptions.SectionName));

        services.AddSingleton<IProjectRepository, XmlProjectRepository>();
        services.AddSingleton<IUserAuthenticator, ConfigUserAuthenticator>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
```

> `XmlProjectRepository` is a singleton so its write-lock is shared process-wide (correct serialization of writes).

- [ ] **Step 3: Build and commit**

```bash
dotnet build && git add -A && git commit -m "feat(infra): DI registration + seed password hash"
```

---

## Phase 4 — Api

### Task 4.1: Result → HTTP mapping helper

**Files:**
- Create: `src/ProjectManager.Api/Common/ResultMapping.cs`

- [ ] **Step 1: Implement mapping (covered by integration tests in 4.4)**

```csharp
using Microsoft.AspNetCore.Http;
using ProjectManager.Application.Common;

namespace ProjectManager.Api.Common;

public static class ResultMapping
{
    /// <summary>Maps a non-success Result to a ProblemDetails IResult. Caller handles success.</summary>
    public static IResult ToProblem(this Result result) => result.Status switch
    {
        ResultStatus.Invalid => Results.ValidationProblem(result.ValidationErrors),
        ResultStatus.NotFound => Results.Problem(result.Message, statusCode: StatusCodes.Status404NotFound),
        ResultStatus.Conflict => Results.Problem(result.Message, statusCode: StatusCodes.Status409Conflict),
        ResultStatus.Unauthorized => Results.Problem(result.Message, statusCode: StatusCodes.Status401Unauthorized),
        _ => Results.Problem("Unexpected result status.", statusCode: StatusCodes.Status500InternalServerError)
    };
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build && git add -A && git commit -m "feat(api): Result to ProblemDetails mapping"
```

### Task 4.2: Contracts DTOs + mapping

**Files:**
- Create: `src/ProjectManager.Contracts/ProjectDto.cs`, `ProjectRequests.cs`, `AuthContracts.cs`

- [ ] **Step 1: Implement DTOs**

`ProjectDto.cs`:

```csharp
namespace ProjectManager.Contracts;

public sealed record ProjectDto(string Id, string Name, string Abbreviation, string Customer);
```

`ProjectRequests.cs`:

```csharp
namespace ProjectManager.Contracts;

public sealed record CreateProjectRequest(string Name, string Abbreviation, string Customer);
public sealed record UpdateProjectRequest(string Name, string Abbreviation, string Customer);
```

`AuthContracts.cs`:

```csharp
namespace ProjectManager.Contracts;

public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt);
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build && git add -A && git commit -m "feat(contracts): HTTP DTOs"
```

### Task 4.3: Endpoints + Program.cs wiring

**Files:**
- Create: `src/ProjectManager.Api/Endpoints/ProjectsEndpoints.cs`, `AuthEndpoints.cs`
- Replace: `src/ProjectManager.Api/Program.cs`
- Modify: `src/ProjectManager.Api/appsettings.json`

- [ ] **Step 1: Implement `AuthEndpoints.cs`**

```csharp
using ProjectManager.Api.Common;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Auth.Login;
using ProjectManager.Contracts;

namespace ProjectManager.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (LoginRequest req, LoginHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new LoginCommand(req.Username, req.Password), ct);
            return result.IsSuccess
                ? Results.Ok(new LoginResponse(result.Value!.Token, result.Value.ExpiresAt))
                : result.ToProblem();
        })
        .AllowAnonymous();
    }
}
```

- [ ] **Step 2: Implement `ProjectsEndpoints.cs`**

```csharp
using ProjectManager.Api.Common;
using ProjectManager.Application.Features.Projects.CreateProject;
using ProjectManager.Application.Features.Projects.DeleteProject;
using ProjectManager.Application.Features.Projects.ListProjects;
using ProjectManager.Application.Features.Projects.UpdateProject;
using ProjectManager.Contracts;
using ProjectManager.Domain;

namespace ProjectManager.Api.Endpoints;

public static class ProjectsEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireAuthorization();

        group.MapGet("/", async (ListProjectsHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ct);
            return Results.Ok(result.Value!.Select(ToDto));
        });

        group.MapGet("/{id}", async (string id, ListProjectsHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ct);
            var project = result.Value!.FirstOrDefault(p => p.Id == id);
            return project is null
                ? Results.Problem($"Project '{id}' was not found.", statusCode: StatusCodes.Status404NotFound)
                : Results.Ok(ToDto(project));
        });

        group.MapPost("/", async (CreateProjectRequest req, CreateProjectHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new CreateProjectCommand(req.Name, req.Abbreviation, req.Customer), ct);
            return result.IsSuccess
                ? Results.Created($"/api/projects/{result.Value!.Id}", ToDto(result.Value))
                : result.ToProblem();
        });

        group.MapPut("/{id}", async (string id, UpdateProjectRequest req, UpdateProjectHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new UpdateProjectCommand(id, req.Name, req.Abbreviation, req.Customer), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        group.MapDelete("/{id}", async (string id, DeleteProjectHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });
    }

    private static ProjectDto ToDto(Project p) => new(p.Id, p.Name, p.Abbreviation, p.Customer);
}
```

- [ ] **Step 3: Replace `Program.cs`**

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProjectManager.Api.Endpoints;
using ProjectManager.Application;
using ProjectManager.Infrastructure;
using ProjectManager.Infrastructure.Auth;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configuration: add the XML config file required by the assignment.
builder.Configuration.AddXmlFile("config.xml", optional: false, reloadOnChange: true);

// Logging: Serilog, structured, console + rolling file.
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/projectmanager-.log", rollingInterval: RollingInterval.Day));

var auth = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()!;

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = auth.Issuer,
            ValidAudience = auth.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.SigningKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

const string ClientCors = "client";
builder.Services.AddCors(o => o.AddPolicy(ClientCors, p => p
    .WithOrigins(builder.Configuration["Cors:ClientOrigin"] ?? "http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseCors(ClientCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapProjectEndpoints();

app.Run();

public partial class Program; // exposed for WebApplicationFactory
```

- [ ] **Step 4: Implement `GlobalExceptionHandler`**

Create `src/ProjectManager.Api/Common/GlobalExceptionHandler.cs`:

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace ProjectManager.Api.Common;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        // Do not leak internals to the client.
        await Results.Problem(
                title: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(context);

        return true;
    }
}
```

- [ ] **Step 5: Set non-secret config in `appsettings.json`** (logging levels, CORS origin) and put the **secret signing key in user-secrets**:

```bash
cd src/ProjectManager.Api
dotnet user-secrets init
dotnet user-secrets set "Auth:SigningKey" "dev-only-signing-key-please-change-min-32-chars-1234"
cd ../..
```

Add to `appsettings.json`:

```json
{
  "Serilog": { "MinimumLevel": { "Default": "Information", "Override": { "Microsoft.AspNetCore": "Warning" } } },
  "Cors": { "ClientOrigin": "http://localhost:5173" }
}
```

- [ ] **Step 6: Build, run, smoke-check**

Run: `dotnet run --project src/ProjectManager.Api`
Then in another shell:
```bash
curl -s -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" -d '{"username":"admin","password":"Admin123!"}'
```
Expected: JSON with a `token`. Then `curl` `/api/projects` without a token → 401; with `Authorization: Bearer <token>` → JSON list of 5 seed projects.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat(api): endpoints, JWT auth, CORS, Serilog, global exception handler"
```

### Task 4.4: Api integration tests

**Files:**
- Create: `tests/ProjectManager.Api.Tests/ProjectsApiTests.cs`
- Create: `tests/ProjectManager.Api.Tests/CustomWebAppFactory.cs`

- [ ] **Step 1: Implement the factory (isolated temp data file + known signing key)**

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ProjectManager.Api.Tests;

public sealed class CustomWebAppFactory : WebApplicationFactory<Program>
{
    // Test-only fixture credential. Defined once; the hash is computed at runtime so
    // there is no pasted magic value and no plaintext-in-comment to drift out of sync.
    public const string TestUser = "admin";
    public const string TestPassword = "Admin123!";

    public string DataFile { get; } = Path.Combine(Path.GetTempPath(), $"pm-api-{Guid.NewGuid():N}.xml");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        File.WriteAllText(DataFile, """
            <?xml version="1.0" encoding="utf-8"?>
            <projects>
              <project id="prj1"><name>Seed</name><abbreviation>SEED</abbreviation><customer>Cust</customer></project>
            </projects>
            """);

        var passwordHash = new PasswordHasher<string>().HashPassword(TestUser, TestPassword);

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ProjectsFilePath"] = DataFile,
                ["Auth:SigningKey"] = "integration-test-signing-key-min-32-characters-1234",
                ["Auth:Username"] = TestUser,
                ["Auth:PasswordHash"] = passwordHash
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (File.Exists(DataFile)) File.Delete(DataFile);
        base.Dispose(disposing);
    }
}
```

> The hash is computed in-fixture from `TestPassword`, so the integration suite is self-contained — no value pasted from Task 3.4, nothing to keep in sync. The login tests reference `CustomWebAppFactory.TestPassword` instead of a literal.

- [ ] **Step 2: Write the integration tests**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ProjectManager.Contracts;
using Xunit;

namespace ProjectManager.Api.Tests;

public class ProjectsApiTests(CustomWebAppFactory factory) : IClassFixture<CustomWebAppFactory>
{
    private async Task<HttpClient> AuthedClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(CustomWebAppFactory.TestUser, CustomWebAppFactory.TestPassword));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    [Fact]
    public async Task GetProjects_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();
        var res = await client.GetAsync("/api/projects");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithBadPassword_Returns401()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/login", new LoginRequest(CustomWebAppFactory.TestUser, "wrong"));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjects_WithToken_ReturnsSeed()
    {
        var client = await AuthedClientAsync();
        var projects = await client.GetFromJsonAsync<List<ProjectDto>>("/api/projects");
        projects.Should().NotBeNull();
        projects!.Should().Contain(p => p.Id == "prj1");
    }

    [Fact]
    public async Task CreateProject_WithBlankName_Returns400ValidationProblem()
    {
        var client = await AuthedClientAsync();
        var res = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("", "X", "C"));
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateThenDeleteProject_Works()
    {
        var client = await AuthedClientAsync();

        var create = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("New", "NEWABBR", "Cust"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<ProjectDto>();

        var delete = await client.DeleteAsync($"/api/projects/{created!.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
```

- [ ] **Step 3: Run to verify pass**

Run: `dotnet test tests/ProjectManager.Api.Tests`
Expected: PASS (5 tests).

- [ ] **Step 4: Run the full suite**

Run: `dotnet test`
Expected: ALL green across all four test projects.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "test(api): integration tests (auth, validation, CRUD)"
```

---

## Phase 5 — Client (Blazor WASM)

> No automated UI tests (backend-focused; documented in the design doc). Verify manually per task. Keep the UI clean and functional.

### Task 5.1: Client config + HTTP/auth plumbing

**Files:**
- Create: `src/ProjectManager.Client/wwwroot/appsettings.json`
- Create: `src/ProjectManager.Client/Auth/JwtAuthenticationStateProvider.cs`
- Create: `src/ProjectManager.Client/Auth/BearerTokenHandler.cs`
- Create: `src/ProjectManager.Client/Services/AuthApiClient.cs`
- Create: `src/ProjectManager.Client/Services/ProjectApiClient.cs`
- Replace: `src/ProjectManager.Client/Program.cs`

- [ ] **Step 1: Add packages**

```bash
dotnet add src/ProjectManager.Client package Microsoft.AspNetCore.Components.Authorization
dotnet add src/ProjectManager.Client package Microsoft.Extensions.Http
```

- [ ] **Step 2: `wwwroot/appsettings.json`** (configurable API base URL — works both locally and in compose):

```json
{ "ApiBaseUrl": "http://localhost:5000" }
```

- [ ] **Step 3: `Auth/JwtAuthenticationStateProvider.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ProjectManager.Client.Auth;

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private string? _token;

    public string? Token => _token;

    public void SetToken(string token)
    {
        _token = token;
        NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));
    }

    public void Clear()
    {
        _token = null;
        NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(BuildState());

    private AuthenticationState BuildState()
    {
        if (string.IsNullOrEmpty(_token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_token);
        var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
```

> In-memory token (cleared on refresh) keeps the example simple and avoids XSS-prone localStorage. Persisting to `sessionStorage` is noted in the design doc as an optional enhancement.

- [ ] **Step 4: `Auth/BearerTokenHandler.cs`**

```csharp
using System.Net.Http.Headers;

namespace ProjectManager.Client.Auth;

public sealed class BearerTokenHandler(JwtAuthenticationStateProvider authState) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(authState.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.Token);
        return base.SendAsync(request, ct);
    }
}
```

- [ ] **Step 5: `Services/AuthApiClient.cs` and `ProjectApiClient.cs`**

`AuthApiClient.cs`:

```csharp
using System.Net.Http.Json;
using ProjectManager.Contracts;

namespace ProjectManager.Client.Services;

public sealed class AuthApiClient(HttpClient http)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var response = await http.PostAsJsonAsync("/auth/login", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<LoginResponse>()
            : null;
    }
}
```

`ProjectApiClient.cs`:

```csharp
using System.Net.Http.Json;
using ProjectManager.Contracts;

namespace ProjectManager.Client.Services;

public sealed class ProjectApiClient(HttpClient http)
{
    public async Task<List<ProjectDto>> GetAllAsync() =>
        await http.GetFromJsonAsync<List<ProjectDto>>("/api/projects") ?? new();

    public async Task<HttpResponseMessage> CreateAsync(CreateProjectRequest req) =>
        await http.PostAsJsonAsync("/api/projects", req);

    public async Task<HttpResponseMessage> UpdateAsync(string id, UpdateProjectRequest req) =>
        await http.PutAsJsonAsync($"/api/projects/{id}", req);

    public async Task<HttpResponseMessage> DeleteAsync(string id) =>
        await http.DeleteAsync($"/api/projects/{id}");
}
```

- [ ] **Step 6: Replace `Program.cs`**

```csharp
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ProjectManager.Client;
using ProjectManager.Client.Auth;
using ProjectManager.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";

builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<BearerTokenHandler>();

builder.Services.AddHttpClient<AuthApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<ProjectApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

await builder.Build().RunAsync();
```

- [ ] **Step 7: Verify build** — `dotnet build src/ProjectManager.Client` → success. Commit:

```bash
git add -A && git commit -m "feat(client): auth state, bearer handler, API clients, DI"
```

### Task 5.2: Pages (Login, Projects list, editor) + auth gating

**Files:**
- Replace: `src/ProjectManager.Client/App.razor` (add `AuthorizeRouteView`)
- Create: `src/ProjectManager.Client/Pages/Login.razor`, `Pages/Projects.razor`, `Pages/ProjectEditor.razor`
- Modify: `src/ProjectManager.Client/Layout/MainLayout.razor` (menu + logout, conditional on auth)

- [ ] **Step 1: Update `App.razor` to gate routes**

```razor
@using Microsoft.AspNetCore.Components.Authorization
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)">
                <NotAuthorized>
                    <RedirectToLogin />
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
        <NotFound>
            <LayoutView Layout="@typeof(Layout.MainLayout)"><p>Not found.</p></LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

- [ ] **Step 2: Create `RedirectToLogin.razor`** in the project root:

```razor
@inject NavigationManager Nav
@code {
    protected override void OnInitialized() => Nav.NavigateTo("/login");
}
```

- [ ] **Step 3: Create `Pages/Login.razor`**

```razor
@page "/login"
@using ProjectManager.Client.Auth
@using ProjectManager.Client.Services
@using ProjectManager.Contracts
@inject AuthApiClient AuthApi
@inject JwtAuthenticationStateProvider AuthState
@inject NavigationManager Nav

<h1>Prihlásenie</h1>

<EditForm Model="@_model" OnValidSubmit="Submit">
    <DataAnnotationsValidator />
    <div>
        <label>Používateľ</label>
        <InputText @bind-Value="_model.Username" />
    </div>
    <div>
        <label>Heslo</label>
        <InputText type="password" @bind-Value="_model.Password" />
    </div>
    <button type="submit" disabled="@_busy">Prihlásiť</button>
    @if (_error is not null)
    {
        <p style="color:red">@_error</p>
    }
</EditForm>

@code {
    private readonly LoginModel _model = new();
    private bool _busy;
    private string? _error;

    private async Task Submit()
    {
        _busy = true; _error = null;
        var response = await AuthApi.LoginAsync(new LoginRequest(_model.Username, _model.Password));
        _busy = false;
        if (response is null) { _error = "Nesprávne meno alebo heslo."; return; }
        AuthState.SetToken(response.Token);
        Nav.NavigateTo("/");
    }

    private sealed class LoginModel
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
```

- [ ] **Step 4: Create `Pages/Projects.razor`** (list + delete + open editor)

```razor
@page "/"
@attribute [Microsoft.AspNetCore.Authorization.Authorize]
@using ProjectManager.Client.Services
@using ProjectManager.Contracts
@inject ProjectApiClient Api

<h1>Projekty</h1>
<button @onclick="@(() => OpenEditor(null))">Nový projekt</button>

@if (_projects is null)
{
    <p>Načítavam…</p>
}
else
{
    <table>
        <thead><tr><th>Názov</th><th>Skratka</th><th>Zákazník</th><th></th></tr></thead>
        <tbody>
        @foreach (var p in _projects)
        {
            <tr>
                <td>@p.Name</td>
                <td>@p.Abbreviation</td>
                <td>@p.Customer</td>
                <td>
                    <button @onclick="@(() => OpenEditor(p))">Upraviť</button>
                    <button @onclick="@(() => Delete(p.Id))">Zmazať</button>
                </td>
            </tr>
        }
        </tbody>
    </table>
}

@if (_editing)
{
    <ProjectEditor Project="_selected" OnSaved="OnSaved" OnCancel="@(() => _editing = false)" />
}

@code {
    private List<ProjectDto>? _projects;
    private bool _editing;
    private ProjectDto? _selected;

    protected override Task OnInitializedAsync() => Load();

    private async Task Load() => _projects = await Api.GetAllAsync();

    private void OpenEditor(ProjectDto? p) { _selected = p; _editing = true; }

    private async Task OnSaved() { _editing = false; await Load(); }

    private async Task Delete(string id)
    {
        var res = await Api.DeleteAsync(id);
        if (res.IsSuccessStatusCode) await Load();
    }
}
```

- [ ] **Step 5: Create `Pages/ProjectEditor.razor`** (create + edit)

```razor
@using ProjectManager.Client.Services
@using ProjectManager.Contracts
@inject ProjectApiClient Api

<div class="editor">
    <h3>@(IsEdit ? "Upraviť projekt" : "Nový projekt")</h3>
    <EditForm Model="@_model" OnValidSubmit="Save">
        <div><label>Názov</label><InputText @bind-Value="_model.Name" /></div>
        <div><label>Skratka</label><InputText @bind-Value="_model.Abbreviation" /></div>
        <div><label>Zákazník</label><InputText @bind-Value="_model.Customer" /></div>
        <button type="submit">Uložiť</button>
        <button type="button" @onclick="OnCancel">Zrušiť</button>
        @if (_error is not null) { <p style="color:red">@_error</p> }
    </EditForm>
</div>

@code {
    [Parameter] public ProjectDto? Project { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private readonly Model _model = new();
    private string? _error;
    private bool IsEdit => Project is not null;

    protected override void OnParametersSet()
    {
        _model.Name = Project?.Name ?? "";
        _model.Abbreviation = Project?.Abbreviation ?? "";
        _model.Customer = Project?.Customer ?? "";
    }

    private async Task Save()
    {
        _error = null;
        var res = IsEdit
            ? await Api.UpdateAsync(Project!.Id, new UpdateProjectRequest(_model.Name, _model.Abbreviation, _model.Customer))
            : await Api.CreateAsync(new CreateProjectRequest(_model.Name, _model.Abbreviation, _model.Customer));

        if (res.IsSuccessStatusCode) { await OnSaved.InvokeAsync(); return; }
        _error = res.StatusCode == System.Net.HttpStatusCode.Conflict
            ? "Projekt s touto skratkou už existuje."
            : "Uloženie zlyhalo. Skontrolujte vstupy.";
    }

    private sealed class Model
    {
        public string Name { get; set; } = "";
        public string Abbreviation { get; set; } = "";
        public string Customer { get; set; } = "";
    }
}
```

- [ ] **Step 6: Add logout + menu to `Layout/MainLayout.razor`**

```razor
@inherits LayoutComponentBase
@using Microsoft.AspNetCore.Components.Authorization
@using ProjectManager.Client.Auth
@inject JwtAuthenticationStateProvider AuthState
@inject NavigationManager Nav

<main class="container">
    <AuthorizeView>
        <Authorized>
            <nav><a href="/">Projekty</a> <button @onclick="Logout">Odhlásiť</button></nav>
        </Authorized>
    </AuthorizeView>
    @Body
</main>

@code {
    private void Logout() { AuthState.Clear(); Nav.NavigateTo("/login"); }
}
```

- [ ] **Step 7: Manual verification**

Run the API: `dotnet run --project src/ProjectManager.Api`
Run the client: `dotnet run --project src/ProjectManager.Client`
Open the client URL. Verify:
1. Unauthenticated visit redirects to `/login`.
2. Login with `admin` / `Admin123!` succeeds and shows 5 projects.
3. Create a new project → appears in list.
4. Edit a project → change persists.
5. Delete a project → removed.
6. Creating a project with a duplicate abbreviation shows the conflict message.
7. Logout returns to login and blocks `/`.

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feat(client): login, projects list, create/edit/delete, auth gating"
```

---

## Phase 6 — Deployment & docs

### Task 6.1: Dockerfiles + compose

**Files:**
- Create: `deploy/api.Dockerfile`, `deploy/client.Dockerfile`, `deploy/nginx.conf`, `deploy/docker-compose.yml`

- [ ] **Step 1: `deploy/api.Dockerfile`**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/ProjectManager.Api/ProjectManager.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProjectManager.Api.dll"]
```

- [ ] **Step 2: `deploy/client.Dockerfile`** (build WASM, serve via nginx)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/ProjectManager.Client/ProjectManager.Client.csproj -c Release -o /app

FROM nginx:alpine AS final
COPY --from=build /app/wwwroot /usr/share/nginx/html
COPY deploy/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

- [ ] **Step 3: `deploy/nginx.conf`** (SPA fallback)

```nginx
server {
    listen 80;
    location / {
        root /usr/share/nginx/html;
        try_files $uri $uri/ /index.html;
    }
}
```

- [ ] **Step 4: `deploy/docker-compose.yml`** (XML data mounted as volume)

```yaml
services:
  api:
    build:
      context: ..
      dockerfile: deploy/api.Dockerfile
    environment:
      - Storage__ProjectsFilePath=/data/projects.xml
      - Auth__SigningKey=compose-signing-key-change-me-min-32-chars-12345
      - Cors__ClientOrigin=http://localhost:8080
    volumes:
      - ../data:/data
    ports:
      - "5000:8080"

  web:
    build:
      context: ..
      dockerfile: deploy/client.Dockerfile
    ports:
      - "8080:80"
    depends_on:
      - api
```

> Note: the WASM client's `ApiBaseUrl` must resolve from the browser. For the simplest setup keep `http://localhost:5000` in `wwwroot/appsettings.json` (the browser reaches the mapped API port). The compose `Cors__ClientOrigin` must match the web origin (`http://localhost:8080`).

- [ ] **Step 5: Verify compose builds and runs**

Run: `docker compose -f deploy/docker-compose.yml up --build`
Open `http://localhost:8080`, log in, confirm CRUD works. Stop with Ctrl+C.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "build: docker-compose (nginx + api) with XML volume"
```

### Task 6.2: Finalize README

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Expand README** with: prerequisites (.NET 10 SDK / Docker), the dev credentials (`admin` / `Admin123!`), how to set the user-secret signing key, three run modes (IDE, `dotnet run` x2, `docker compose`), configuration explanation (`config.xml`, env overrides), and a link to the design doc. Note the `~8h` scope and that Aspire/observability is a documented future layer.

- [ ] **Step 2: Final full build + test**

Run: `dotnet build && dotnet test`
Expected: build succeeds, all tests green.

- [ ] **Step 3: Commit and push**

```bash
git add -A && git commit -m "docs: finalize README with run/config instructions"
git push
```

---

## Self-Review

**Spec coverage** (design doc → tasks):
- Login / auth → Tasks 2.7, 3.2, 3.3, 4.3 (JWT, hashed password, `[Authorize]`). ✅
- Project list + CRUD → Tasks 2.3–2.6, 4.3, 5.2. ✅
- XML-only storage, swappable → Task 2.2 (port) + 3.1 (XML adapter) + 3.4 (DI swap point). ✅
- Three-layer / Clean → project structure (Phase 0) + inward references (Task 0.2). ✅
- Config in XML + Options → Task 0.4 (`config.xml`) + 4.3 (`AddXmlFile`) + Options in 3.1/3.2. ✅
- Logging → Task 4.3 (Serilog, request logging, global handler) + warnings in repo (3.1). ✅
- Error handling + validation → `Result` (2.1), FluentValidation (2.4–2.7), ProblemDetails (4.1, 4.3), XML errors (3.1). ✅
- Tests → Domain (1.1), Application (2.x), Infrastructure incl. concurrency (3.x), Api integration (4.4). ✅
- Run/deploy → Docker compose (6.1), README (6.2), local IDE (manual steps). ✅
- Aspire → intentionally deferred (design doc §11/§14); not a task. ✅

**Placeholder scan:** One intentional, clearly-flagged manual value — the generated password hash — appears in `config.xml` (Task 3.4 Step 1) and `CustomWebAppFactory` (Task 4.4 Step 1). Both instruct to paste the same hash generated in Task 3.4. No silent TBDs.

**Type consistency:** `IProjectRepository` (GetAllAsync/GetByIdAsync/AddAsync/UpdateAsync/DeleteAsync/NextIdAsync), `Result`/`Result<T>`/`ResultStatus`, `AuthenticatedUser`, `TokenResult`, command/handler names, and Contracts DTOs are used identically across handler, infra, api, and client tasks. Handlers return: `Result<Project>` (Create), `Result` (Update/Delete), `Result<IReadOnlyList<Project>>` (List), `Result<TokenResult>` (Login) — matched by their tests and endpoints.
