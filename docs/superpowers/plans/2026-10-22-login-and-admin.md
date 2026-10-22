# Login, Users and Admin Menu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Protect the whole app (UI and API) with username/password login, add Admin/User roles, admin-only Settings and Admin → Users menus, a bootstrap admin from appsettings, HTTPS on the Web host, and encrypted SQL passwords for data read servers.

**Architecture:** Cookie authentication lives in the Api. The Web host stays a pass-through proxy; it forwards cookies already and will also forward the client IP. A fallback authorization policy protects every Api endpoint, and an `Admin` policy guards settings and user management. Every request checks the user's `SecurityStamp` against the DB, so a block or password reset ends sessions immediately. The React client asks `/api/auth/me` to decide between the login page, the forced password change and the app, and builds the menu from the user's role.

**Tech Stack:** .NET 10 Minimal API, EF Core 10 (SQL Server), ASP.NET Core cookie auth + Data Protection, `PasswordHasher<T>` from `Microsoft.Extensions.Identity.Core`, `Microsoft.AspNetCore.RateLimiting`, React 19 + TypeScript + Vite, xUnit v3 + `WebApplicationFactory` against LocalDB.

**Spec:** `docs/superpowers/specs/2026-10-22-login-and-admin-design.md` (in Slovak). Read it before starting any task.

## Global Constraints

- Work on a new branch (e.g. `feature/login-admin`). The working tree has unrelated uncommitted changes: always `git add` explicit paths, never `git add -A` / `git add .`.
- Packages are managed centrally in `Directory.Packages.props` with `CentralPackageTransitivePinningEnabled`. Add new `PackageVersion` entries there; csproj `PackageReference`s carry no version. New versions: `Microsoft.Extensions.Identity.Core` 10.0.13, `Microsoft.AspNetCore.DataProtection.Abstractions` 10.0.13, `Microsoft.AspNetCore.Mvc.Testing` 10.0.13, `xunit.v3` 4.0.2, `xunit.runner.visualstudio` 4.0.1, `Microsoft.NET.Test.Sdk` 18.11.0.
- Follow existing patterns: entities derive from `KrosExtension.Domain.Common.Entity`, have a private parameterless ctor and private setters, and use `DateTimeOffset`. EF configs go in `Infrastructure/Persistence/Configurations/` (picked up by `ApplyConfigurationsFromAssembly`). Endpoints use static `Map*Endpoints` extension classes with `MapGroup` / `WithTags` / `WithName`. Infrastructure services take `ApplicationDbContext` directly via primary constructor, like `SettingsRepository`. Error responses use `Results.BadRequest("text")` / `Results.Conflict("text")`, because the frontend shows `await response.text()`.
- Roles: exactly two, `User` and `Admin`. Role claim values are `"User"` / `"Admin"`.
- Password rule: at least **10** characters; the new password must differ from the current one.
- Lockout: **5** failed logins → locked for **15 minutes**.
- Session: sliding expiration, **8 hours**; cookie `HttpOnly`, `SameSite=Strict`, name `LedgerPulse.Auth`.
- API never redirects for auth: unauthenticated → **401**, wrong role → **403**, locked with a correct password → **423**.
- Login error copy (Slovak, exact): `Nesprávne meno alebo heslo`, `Účet je dočasne zamknutý. Skús to neskôr.`, `Zabudnuté heslo? Kontaktuj admina.`
- Anonymous endpoints: only `POST /api/auth/login` and `/health`.
- Api listens only on `http://localhost:5184`. Web serves HTTPS on port **5284** in production, with no HTTP listener.
- Swagger only when `app.Environment.IsDevelopment()`.
- Never log passwords (bootstrap, temporary or user).
- Deployment target: Windows services on Windows Server only. Leave `src/KrosExtension.Web/Dockerfile` untouched.

## Review Focus

1. **Username case/whitespace:** `" Admin "` at login must hit the `admin` account, and creating `JNovak` when `jnovak` exists must be refused as a duplicate. Tests: Task 3 `Login_IsCaseAndWhitespaceInsensitive`, Task 4 `CreateUser_DuplicateDifferentCase_Returns409`.
2. **One user, two browsers:** when the admin blocks a user or resets their password, every existing session of that user must stop working, not just the newest. Test: Task 4 `Block_InvalidatesAllExistingSessions`.
3. **Forced password change then continue:** after `change-password` the *same* browser session must keep working without logging in again. The stamp changes, so the endpoint must re-issue the cookie. Test: Task 3 `ChangePassword_ReissuesCookie_SameClientCanContinue`.
4. **Everyone behind the proxy looks like 127.0.0.1:** without the forwarded client IP, one person mistyping would rate-limit the whole office. Test: Task 5 `LoginRateLimit_IsPartitionedByForwardedClientIp`.
5. **Fresh install without bootstrap password:** the Api must still start (health 200) and simply refuse logins, not crash the Windows service. Test: Task 2 `Bootstrap_NoAdminAndNoPassword_AppStartsAndLogsError`.

---

## File Map

| File | Change |
|---|---|
| `tests/KrosExtension.Api.Tests/*` | New test project (xUnit v3, `WebApplicationFactory`, LocalDB) |
| `src/KrosExtension.Domain/Users/AppUser.cs`, `UserRole.cs` | New entity + enum |
| `src/KrosExtension.Application/Users/*` | Service interfaces, result types, `PasswordPolicy`, `BootstrapOptions` |
| `src/KrosExtension.Infrastructure/Users/*` | `UserAccountService`, `UserAdministrationService`, `AdminBootstrapper`, `TemporaryPasswordGenerator` |
| `src/KrosExtension.Infrastructure/Persistence/Configurations/AppUserConfiguration.cs` | New EF config |
| `src/KrosExtension.Infrastructure/Persistence/Migrations/*_AddAppUsers.cs` | New migration |
| `src/KrosExtension.Infrastructure/Persistence/ApplicationDbContext.cs` | `Users` DbSet; later secret converter |
| `src/KrosExtension.Infrastructure/Persistence/SecretValueConverter.cs` | New (Task 9) |
| `src/KrosExtension.Infrastructure/Configuration/DataReadServerSecretMigrator.cs` | New (Task 9) |
| `src/KrosExtension.Api/Auth/*` | `AuthenticationSetup`, `AuthPolicies`, `AuthOptions`, `CookieSessionValidator`, `ClaimsFactory` |
| `src/KrosExtension.Api/Endpoints/AuthEndpoints.cs`, `AdminUserEndpoints.cs` | New |
| `src/KrosExtension.Api/Endpoints/*Endpoints.cs` (existing) | Admin policy on settings endpoints |
| `src/KrosExtension.Api/Program.cs`, `appsettings*.json` | Wiring, localhost URL, Swagger dev-only |
| `src/KrosExtension.Web/Program.cs`, `appsettings.json` | HSTS, `X-Forwarded-For` |
| `src/KrosExtension.Web/ClientApp/src/api.ts`, `auth.tsx`, `admin/UsersPage.tsx` | New |
| `src/KrosExtension.Web/ClientApp/src/main.tsx`, `styles.css` | Auth gate, role-based menu, `apiFetch` |
| `scripts/Install-Service.ps1`, `readme_deploy.md` | Production config files, HTTPS, first login, key backup |

---

### Task 1: Test project and `AppUser` domain entity

**Files:**
- Create: `tests/KrosExtension.Api.Tests/KrosExtension.Api.Tests.csproj`
- Create: `src/KrosExtension.Domain/Users/UserRole.cs`, `src/KrosExtension.Domain/Users/AppUser.cs`
- Test: `tests/KrosExtension.Api.Tests/Domain/AppUserTests.cs`
- Modify: `Directory.Packages.props`, `KrosExtension.sln`

**Interfaces:**
- Produces:
  - `enum UserRole { User = 1, Admin = 2 }` (namespace `KrosExtension.Domain.Users`)
  - `sealed class AppUser : Entity` with:
    - `AppUser(string userName, string passwordHash, UserRole role, DateTimeOffset now)` → `MustChangePassword = true`, new `SecurityStamp`, `CreatedAt = now`, `UserName = userName.Trim()`, `NormalizedUserName = Normalize(userName)`
    - props `UserName`, `NormalizedUserName`, `PasswordHash`, `Role`, `IsBlocked`, `MustChangePassword`, `SecurityStamp` (string, `Guid.NewGuid().ToString("N")`), `FailedLoginCount`, `LockoutEndsAt` (`DateTimeOffset?`), `CreatedAt`, `LastLoginAt?`, `PasswordChangedAt?`
    - `const int MaxFailedLogins = 5`; `static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15)`
    - `static string Normalize(string userName)` → `userName.Trim().ToUpperInvariant()`
    - `bool IsLockedOut(DateTimeOffset now)`
    - `void RegisterFailedLogin(DateTimeOffset now)`: increments; on reaching 5 sets `LockoutEndsAt = now + 15 min` and resets the count to 0
    - `void RegisterSuccessfulLogin(DateTimeOffset now)`: count 0, lockout null, `LastLoginAt = now`
    - `void SetPasswordHash(string hash)`: rehash only, no stamp change
    - `void ChangePassword(string newHash, DateTimeOffset now)`: `MustChangePassword = false`, `PasswordChangedAt = now`, new stamp
    - `void ResetPassword(string temporaryHash, DateTimeOffset now)`: `MustChangePassword = true`, clears lockout, new stamp
    - `void Block()` (new stamp), `void Unblock()`, `void ChangeRole(UserRole role)` (new stamp only if it changed)
    - `void RestoreAsBootstrapAdmin(string temporaryHash, DateTimeOffset now)`: `Unblock()`, role Admin, `ResetPassword(...)`

- [ ] **Step 1: Create the test project**

`tests/KrosExtension.Api.Tests/KrosExtension.Api.Tests.csproj`: `Microsoft.NET.Sdk.Web` is not needed. Use `Microsoft.NET.Sdk`, `<OutputType>Exe</OutputType>` (required by xUnit v3) and `<IsPackable>false</IsPackable>`. References: `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Microsoft.AspNetCore.Mvc.Testing`, plus a ProjectReference to `src/KrosExtension.Api`. Add the `PackageVersion` entries listed in Global Constraints. Run `dotnet sln KrosExtension.sln add tests/KrosExtension.Api.Tests/KrosExtension.Api.Tests.csproj`.

- [ ] **Step 2: Write the failing tests** in `Domain/AppUserTests.cs`

```csharp
[Fact] void Ctor_TrimsNameAndRequiresPasswordChange()
  // new AppUser("  JNovak ", "h", UserRole.User, T0): UserName == "JNovak", NormalizedUserName == "JNOVAK", MustChangePassword, SecurityStamp not empty
[Fact] void FifthFailedLogin_LocksForFifteenMinutes()
  // 4x RegisterFailedLogin(T0) → !IsLockedOut(T0); 5th → IsLockedOut(T0), IsLockedOut(T0.AddMinutes(14)), !IsLockedOut(T0.AddMinutes(15))
[Fact] void SuccessfulLogin_ClearsFailures()
  // 3 failures, RegisterSuccessfulLogin(T0) → FailedLoginCount == 0, LastLoginAt == T0
[Fact] void ChangePassword_ClearsMustChangeAndRotatesStamp()
[Fact] void ResetPassword_SetsMustChange_ClearsLockout_RotatesStamp()
[Fact] void Block_RotatesStamp()
[Fact] void ChangeRole_SameRole_KeepsStamp()
[Fact] void RestoreAsBootstrapAdmin_UnblocksPromotesAndForcesChange()
```

- [ ] **Step 3: Run the tests and confirm they fail to compile**

Run: `dotnet test tests/KrosExtension.Api.Tests`
Expected: build error, `AppUser` not found.

- [ ] **Step 4: Implement `UserRole` and `AppUser`** per the Interfaces block.

- [ ] **Step 5: Run the tests**

Run: `dotnet test tests/KrosExtension.Api.Tests`
Expected: 8 passed.

- [ ] **Step 6: Commit**

```bash
git add Directory.Packages.props KrosExtension.sln tests/KrosExtension.Api.Tests src/KrosExtension.Domain/Users
git commit -m "feat: add AppUser entity and test project"
```

---

### Task 2: Persistence, test host and admin bootstrap

**Files:**
- Create: `src/KrosExtension.Infrastructure/Persistence/Configurations/AppUserConfiguration.cs`
- Create: migration `AddAppUsers` (generated)
- Create: `src/KrosExtension.Application/Users/BootstrapOptions.cs`, `IAdminBootstrapper.cs`
- Create: `src/KrosExtension.Infrastructure/Users/AdminBootstrapper.cs`
- Modify: `ApplicationDbContext.cs` (add `public DbSet<AppUser> Users => Set<AppUser>();`), `Infrastructure/DependencyInjection.cs`, `Infrastructure.csproj` (add `Microsoft.Extensions.Identity.Core`), `Api/Program.cs`, `Api/appsettings.json`
- Test: `tests/KrosExtension.Api.Tests/ApiFactory.cs`, `tests/KrosExtension.Api.Tests/BootstrapTests.cs`

**Interfaces:**
- Consumes: `AppUser`, `UserRole` (Task 1).
- Produces:
  - `sealed class BootstrapOptions { const string SectionName = "Bootstrap"; string AdminUserName = "admin"; string AdminPassword = ""; }`
  - `interface IAdminBootstrapper { Task RunAsync(CancellationToken cancellationToken = default); }`
  - DI: `IPasswordHasher<AppUser>` → `PasswordHasher<AppUser>` (singleton), `TimeProvider.System` (singleton, `TryAddSingleton`), `IAdminBootstrapper` (scoped).
  - Table `AppUsers`: `UserName` max 128 required; `NormalizedUserName` max 128 required with a **unique index**; `PasswordHash` max 512 required; `Role` `HasConversion<int>()`; `SecurityStamp` max 64 required.
  - Test helper `ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime` with:
    - `const string BootstrapAdminPassword = "Bootstrap-Pass-1"`, `const string AdminPassword = "Admin-Pass-123"`
    - `HttpClient CreateCookieClient()` (`HandleCookies = true`)
    - `Task<HttpClient> LoginAsAdminAsync()`: logs in with the bootstrap password and changes it to `AdminPassword` once per factory; later calls log in with `AdminPassword`. Available from Task 3.
    - `virtual void ConfigureSettings(IDictionary<string,string?> s)` so derived factories can override settings.
  - `public partial class Program;` appended to `Api/Program.cs`.

- [ ] **Step 1: Write `ApiFactory`**

  - Settings via `builder.UseSetting(...)`: `ConnectionStrings:DefaultConnection` = `Server=(localdb)\MSSQLLocalDB;Database=KrosExtension_Tests_{Guid:N};Trusted_Connection=True;TrustServerCertificate=True`, `Bootstrap:AdminUserName=admin`, `Bootstrap:AdminPassword=Bootstrap-Pass-1`, `Auth:CookieSecurePolicy=SameAsRequest`, `Auth:LoginRateLimitPerMinute=1000`, `DataProtection:KeysPath` = a temp directory.
  - Environment `Testing`.
  - In `ConfigureServices`, remove the `DataRefreshBackgroundService` hosted service registration.
  - `DisposeAsync` deletes the test database (`EnsureDeletedAsync` via a scope).
  - If `UseSetting` values are not visible to `Program` while it builds services (the connection string is read eagerly in `AddInfrastructure`), set them as process env vars (`ConnectionStrings__DefaultConnection` etc.) in the factory constructor instead.

- [ ] **Step 2: Write the failing tests** in `BootstrapTests.cs` (each test owns its own factory instance, so the DB is fresh)

```csharp
[Fact] Bootstrap_NoAdmin_CreatesAdminThatMustChangePassword()
  // after start: scope → ApplicationDbContext.Users single: NormalizedUserName "ADMIN", Role Admin, MustChangePassword
[Fact] Bootstrap_ActiveAdminExists_DoesNothing()
  // start once, change the stored hash marker (e.g. call ChangePassword on the entity + save), restart the host on the same DB → hash unchanged
[Fact] Bootstrap_AdminBlocked_RestoresAndUnlocks()
  // block the admin + set lockout, restart → !IsBlocked, !IsLockedOut, MustChangePassword, Role Admin
[Fact] Bootstrap_NoAdminAndNoPassword_AppStartsAndLogsError()
  // AdminPassword "" → GET /health == 200, Users empty
```

For the restart tests, add an `ApiFactory` constructor overload that takes an existing database name.

- [ ] **Step 3: Run the tests and confirm they fail**

Run: `dotnet test tests/KrosExtension.Api.Tests --filter BootstrapTests`
Expected: FAIL (no `Users` DbSet / no bootstrap).

- [ ] **Step 4: Add `AppUserConfiguration`, the DbSet and the migration**

Run: `dotnet ef migrations add AddAppUsers --project src/KrosExtension.Infrastructure --startup-project src/KrosExtension.Api --output-dir Persistence/Migrations`
Expected: migration creating `AppUsers` with the unique index on `NormalizedUserName`.

- [ ] **Step 5: Implement `AdminBootstrapper.RunAsync`** following spec section "Bootstrap prvého admina":
  - any user with `Role == Admin && !IsBlocked` → do nothing; log a warning if `AdminPassword` is not empty
  - else if `AdminPassword` is empty → log an error and return
  - else find by `NormalizedUserName`: if found, `RestoreAsBootstrapAdmin(hash, now)`; if not, create `new AppUser(name, hash, UserRole.Admin, now)`. Save.
  - Never log the password.

  Wire it up: register it in `AddInfrastructure` together with `services.Configure<BootstrapOptions>(configuration.GetSection(BootstrapOptions.SectionName))`. In `Program.cs`, call `RunAsync` in the existing startup scope right after `Database.Migrate()`. Add `"Bootstrap": { "AdminUserName": "admin", "AdminPassword": "" }` to `Api/appsettings.json`.

- [ ] **Step 6: Run the tests**

Run: `dotnet test tests/KrosExtension.Api.Tests`
Expected: all pass.

- [ ] **Step 7: Commit**

```bash
git add src/KrosExtension.Domain src/KrosExtension.Application/Users src/KrosExtension.Infrastructure src/KrosExtension.Api/Program.cs src/KrosExtension.Api/appsettings.json tests/KrosExtension.Api.Tests Directory.Packages.props
git commit -m "feat: persist users and bootstrap first admin from appsettings"
```

---

### Task 3: Cookie authentication and `/api/auth/*`

**Files:**
- Create: `src/KrosExtension.Application/Users/IUserAccountService.cs`, `LoginResult.cs`, `PasswordPolicy.cs`
- Create: `src/KrosExtension.Infrastructure/Users/UserAccountService.cs`
- Create: `src/KrosExtension.Api/Auth/AuthOptions.cs`, `AuthPolicies.cs`, `ClaimsFactory.cs`, `CookieSessionValidator.cs`, `AuthenticationSetup.cs`
- Create: `src/KrosExtension.Api/Endpoints/AuthEndpoints.cs`
- Modify: `Api/Program.cs`, `Api/appsettings.json`, `Api/appsettings.Development.json`, `Infrastructure/DependencyInjection.cs`
- Test: `tests/KrosExtension.Api.Tests/AuthTests.cs`

**Interfaces:**
- Consumes: `AppUser`, `IPasswordHasher<AppUser>`, `TimeProvider`, `ApiFactory`.
- Produces:
  - `enum LoginStatus { Success, InvalidCredentials, LockedOut }`; `record LoginResult(LoginStatus Status, AppUser? User)`
  - `enum PasswordChangeStatus { Success, WrongCurrentPassword, TooShort, SameAsCurrent }`
  - `static class PasswordPolicy { const int MinLength = 10; static bool IsLongEnough(string password); }`
  - `interface IUserAccountService`:
    - `Task<LoginResult> LoginAsync(string userName, string password, CancellationToken ct = default)`
    - `Task<(PasswordChangeStatus Status, AppUser? User)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)`
    - `Task<AppUser?> FindByIdAsync(Guid userId, CancellationToken ct = default)`
  - `static class AuthPolicies { const string Admin = "Admin"; const string PasswordChange = "PasswordChange"; }`
  - `static class AppClaimTypes { const string SecurityStamp = "security_stamp"; const string MustChangePassword = "must_change_password"; }` (in `ClaimsFactory.cs`)
  - `static ClaimsPrincipal ClaimsFactory.Create(AppUser user)`: `NameIdentifier` = Id, `Name` = UserName, `Role` = `Role.ToString()`, `security_stamp`, and `must_change_password = "true"` only when set
  - `sealed class AuthOptions { SectionName = "Auth"; CookieSecurePolicy CookieSecurePolicy = Always; int LoginRateLimitPerMinute = 20; }`
  - `static IServiceCollection AddAppAuthentication(this IServiceCollection, IConfiguration)` and `static WebApplication UseAppAuthentication(this WebApplication)`
  - HTTP: `POST /api/auth/login {userName,password}` → 200 `{id,userName,role,mustChangePassword}` / 401 / 423; `POST /api/auth/logout` → 204; `GET /api/auth/me` → 200 same shape / 401; `POST /api/auth/change-password {currentPassword,newPassword}` → 200 same shape / 400 text

- [ ] **Step 1: Write the failing tests** in `AuthTests.cs` (`IClassFixture<ApiFactory>`; each test creates its own users via a scope on `ApplicationDbContext` + `IPasswordHasher<AppUser>`, with unique names)

```csharp
Anonymous_ProtectedEndpoint_Returns401()            // GET /api/system/info → 401, no redirect (Location header absent)
Anonymous_Health_Returns200()
Login_WrongPassword_Returns401WithGenericMessage()  // body == "Nesprávne meno alebo heslo"
Login_UnknownUser_Returns401WithSameMessage()
Login_BlockedUser_Returns401()
Login_IsCaseAndWhitespaceInsensitive()             // user "jnovak", login " JNovak " → 200
Login_FiveWrongPasswords_ThenCorrect_Returns423()   // body == "Účet je dočasne zamknutý. Skús to neskôr."
Login_LockedOut_WrongPassword_Returns401()          // locked account + wrong password → 401, not 423
Me_AfterLogin_ReturnsUser()
Logout_ThenMe_Returns401()
MustChangePassword_BlocksOtherEndpoints()           // fresh user (MustChangePassword) → /api/system/info 403, /api/auth/me 200
ChangePassword_TooShort_Returns400()                // "short" → 400
ChangePassword_SameAsCurrent_Returns400()
ChangePassword_WrongCurrent_Returns400()
ChangePassword_ReissuesCookie_SameClientCanContinue() // after 200 the same client: /api/system/info 200, /api/auth/me mustChangePassword false
StaleSecurityStamp_Returns401()                     // log in, rotate the stamp in DB (Block+Unblock), same client → /api/auth/me 401
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `dotnet test tests/KrosExtension.Api.Tests --filter AuthTests`
Expected: FAIL (404 / 200 instead of 401).

- [ ] **Step 3: Implement `UserAccountService`**

`LoginAsync` logic, in this order:
1. Look up by `AppUser.Normalize(userName)`.
2. Not found → run `VerifyHashedPassword` against a fixed dummy hash (equal timing) → `InvalidCredentials`.
3. Blocked → `InvalidCredentials`.
4. Locked out → `LockedOut` if the password verifies, else `InvalidCredentials`. Do not count failures while locked.
5. Wrong password → `RegisterFailedLogin`, save → `InvalidCredentials`.
6. Correct → `RegisterSuccessfulLogin`. On `SuccessRehashNeeded` → `SetPasswordHash(rehash)`. Save → `Success`.

`ChangePasswordAsync` checks, in this order: current password verifies, `PasswordPolicy.IsLongEnough`, new ≠ current. Then `ChangePassword(hash, now)` and save.

- [ ] **Step 4: Implement the auth wiring** in `AuthenticationSetup`
  - Data Protection: `AddDataProtection().SetApplicationName("KrosExtension").PersistKeysToFileSystem(new DirectoryInfo(keysPath))`, plus `.ProtectKeysWithDpapi(protectToLocalMachine: true)` when `OperatingSystem.IsWindows()`. `keysPath` = config `DataProtection:KeysPath`, default `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "KrosExtension", "keys")`.
  - Cookie: name `LedgerPulse.Auth`, `HttpOnly`, `SameSite=Strict`, `SecurePolicy` from `AuthOptions`, `ExpireTimeSpan = 8h`, `SlidingExpiration = true`. `Events.OnRedirectToLogin` → 401, `OnRedirectToAccessDenied` → 403, `OnValidatePrincipal = CookieSessionValidator.ValidateAsync`.
  - `CookieSessionValidator.ValidateAsync`: load the user by `NameIdentifier`. If it is missing, blocked or the stamp differs → `RejectPrincipal()` + `SignOutAsync`.
  - Authorization:
    - `FallbackPolicy` = authenticated AND no `must_change_password` claim
    - `Admin` = same + `RequireRole("Admin")`
    - `PasswordChange` = authenticated only
  - `UseAppAuthentication`: `UseAuthentication()`, `UseAuthorization()`.
  - `/health`: `.AllowAnonymous()`.
  - `appsettings.json`: `"Auth": { "CookieSecurePolicy": "Always", "LoginRateLimitPerMinute": 20 }`. `appsettings.Development.json`: `"Auth": { "CookieSecurePolicy": "SameAsRequest" }`.

- [ ] **Step 5: Implement `AuthEndpoints`** (group `/api/auth`, tag `Auth`)
  - login: `.AllowAnonymous()`, maps `LoginStatus` to 200 + `SignInAsync(ClaimsFactory.Create(user))` / 401 / 423 with the exact copy
  - logout, me, change-password: `.RequireAuthorization(AuthPolicies.PasswordChange)`
  - change-password on success calls `SignInAsync` again with the updated user (fresh stamp, no must-change claim)
  - 400 texts:
    - `Súčasné heslo nie je správne.`
    - `Heslo musí mať aspoň 10 znakov.`
    - `Nové heslo sa musí líšiť od súčasného.`

  Call `MapAuthEndpoints()` in `Program.cs`.

- [ ] **Step 6: Run all tests**

Run: `dotnet test tests/KrosExtension.Api.Tests`
Expected: all pass.

- [ ] **Step 7: Commit**

```bash
git add src/KrosExtension.Application/Users src/KrosExtension.Infrastructure src/KrosExtension.Api tests/KrosExtension.Api.Tests
git commit -m "feat: cookie authentication with lockout, forced password change and session validation"
```

---

### Task 4: Admin user management API

**Files:**
- Create: `src/KrosExtension.Application/Users/IUserAdministrationService.cs`, `UserAdminResult.cs`
- Create: `src/KrosExtension.Infrastructure/Users/UserAdministrationService.cs`, `TemporaryPasswordGenerator.cs`
- Create: `src/KrosExtension.Api/Endpoints/AdminUserEndpoints.cs`
- Modify: `Infrastructure/DependencyInjection.cs`, `Api/Program.cs`
- Test: `tests/KrosExtension.Api.Tests/AdminUserTests.cs`

**Interfaces:**
- Consumes: `AppUser`, `AuthPolicies.Admin`, `ApiFactory.LoginAsAdminAsync()`.
- Produces:
  - `enum UserAdminError { None, NotFound, EmptyUserName, DuplicateUserName, CannotModifySelf, LastActiveAdmin }`
  - `record UserAdminResult(UserAdminError Error, AppUser? User = null, string? TemporaryPassword = null)`
  - `interface IUserAdministrationService`:
    - `Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken ct = default)` (ordered by `UserName`)
    - `Task<UserAdminResult> CreateUserAsync(string userName, UserRole role, CancellationToken ct = default)`
    - `Task<UserAdminResult> ResetPasswordAsync(Guid userId, CancellationToken ct = default)`
    - `Task<UserAdminResult> BlockAsync(Guid actingUserId, Guid userId, CancellationToken ct = default)`
    - `Task<UserAdminResult> UnblockAsync(Guid userId, CancellationToken ct = default)`
    - `Task<UserAdminResult> ChangeRoleAsync(Guid actingUserId, Guid userId, UserRole role, CancellationToken ct = default)`
  - `static string TemporaryPasswordGenerator.Generate()`: 12 chars via `RandomNumberGenerator.GetString` from `ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789`
  - HTTP (group `/api/admin/users`, tag `Admin users`, `.RequireAuthorization(AuthPolicies.Admin)`):
    - `GET /` → `[{id,userName,role,isBlocked,mustChangePassword,isLockedOut,createdAt,lastLoginAt}]`
    - `POST / {userName, role}` → 201 `{user, temporaryPassword}`
    - `POST /{id}/reset-password` → 200 `{temporaryPassword}`
    - `POST /{id}/block`, `POST /{id}/unblock` → 204
    - `PUT /{id}/role {role}` → 204
    - Errors: NotFound → 404; EmptyUserName → 400 `Meno je povinné.`; DuplicateUserName → 409 `Používateľ s týmto menom už existuje.`; CannotModifySelf → 400 `Nemôžeš zablokovať ani zmeniť rolu sám sebe.`; LastActiveAdmin → 400 `Musí zostať aspoň jeden aktívny admin.`

- [ ] **Step 1: Write the failing tests** in `AdminUserTests.cs`

```csharp
User_CannotAccessAdminUsers_Returns403()
Admin_ListsUsers_WithoutPasswordHash()               // JSON has no "passwordHash" property
CreateUser_ReturnsTemporaryPassword_ThatForcesChange() // login with the temp password → me.mustChangePassword true
CreateUser_DuplicateDifferentCase_Returns409()       // "pkral" then "PKral"
CreateUser_EmptyName_Returns400()
ResetPassword_OldPasswordFails_TempWorks_AndOldSessionEnds()
Block_InvalidatesAllExistingSessions()               // two clients logged in as the same user → both 401 after block
Block_Self_Returns400()
ChangeRole_Self_Returns400()
Unblock_AllowsLoginAgain()
ChangeRole_UserToAdmin_GrantsAdminAccess()           // the user logs in again → GET /api/admin/users 200
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `dotnet test tests/KrosExtension.Api.Tests --filter AdminUserTests`
Expected: FAIL (404).

- [ ] **Step 3: Implement `UserAdministrationService` and `TemporaryPasswordGenerator`**
  - Block and ChangeRole return `CannotModifySelf` when `actingUserId == userId`.
  - Demoting or blocking the last active admin (`Role == Admin && !IsBlocked`, count ≤ 1) returns `LastActiveAdmin`.
  - Duplicate check uses `NormalizedUserName`. Also catch `DbUpdateException` from the unique index as `DuplicateUserName`.

- [ ] **Step 4: Implement `AdminUserEndpoints`.** The acting user id comes from the `NameIdentifier` claim. Call `MapAdminUserEndpoints()` in `Program.cs`.

- [ ] **Step 5: Run all tests**

Run: `dotnet test tests/KrosExtension.Api.Tests`
Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add src/KrosExtension.Application/Users src/KrosExtension.Infrastructure/Users src/KrosExtension.Infrastructure/DependencyInjection.cs src/KrosExtension.Api tests/KrosExtension.Api.Tests
git commit -m "feat: admin user management API"
```

---

### Task 5: Protect existing endpoints, localhost-only Api, login rate limit

**Files:**
- Modify: `src/KrosExtension.Api/Endpoints/DataReadServerEndpoints.cs`, `DatabaseEndpoints.cs`, `CompanyEndpoints.cs`, `ProductCompanyEndpoints.cs`
- Modify: `src/KrosExtension.Api/Auth/AuthenticationSetup.cs`, `Api/Program.cs`, `Api/appsettings.json`
- Test: `tests/KrosExtension.Api.Tests/EndpointAccessTests.cs`, `tests/KrosExtension.Api.Tests/RateLimitTests.cs`

**Interfaces:**
- Consumes: `AuthPolicies.Admin`, `AuthOptions.LoginRateLimitPerMinute`.
- Produces: rate limiter policy name `"login"`, applied to `POST /api/auth/login` with `.RequireRateLimiting("login")`.

- [ ] **Step 1: Write the failing tests**

`EndpointAccessTests` (`[Theory]` over the admin list):

| Method + path | User | Admin |
|---|---|---|
| `GET /api/data-read-servers` | 403 | 200 |
| `GET /api/omega/refresh-settings`, `GET /api/olymp/refresh-settings` | 403 | 200 |
| `PUT /api/omega/refresh-settings` | 403 | not 403 |
| `GET /api/omega/companies/tracked` | 403 | 200 |
| `GET /api/omega/companies/discover` | 403 | not 403 |
| `GET /api/databases/configured` | 403 | 200 |
| `GET /api/companies/data-sources` | 403 | 200 |

Plus:
- `User_CanReadDashboardsAndSystemInfo()`: `GET /api/omega/dashboard/companies`, `GET /api/olymp/dashboard/companies`, `GET /api/system/info` → 200 for a User
- `User_CanTriggerManualUpdate_NotForbidden()`: `POST /api/omega/companies/update-data` `{databaseName:null}` → status ≠ 401/403

`RateLimitTests`: a derived factory overriding `Auth:LoginRateLimitPerMinute=2`:
- `LoginRateLimit_ExceededFromSameIp_Returns429()`: 3 logins with the same `X-Forwarded-For: 10.0.0.5` → the 3rd is 429
- `LoginRateLimit_IsPartitionedByForwardedClientIp()`: 2 from `10.0.0.5`, then 1 from `10.0.0.6` → not 429

The TestServer remote IP is loopback, which matches the trusted proxy.

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `dotnet test tests/KrosExtension.Api.Tests --filter "EndpointAccessTests|RateLimitTests"`
Expected: FAIL (User gets 200 on admin endpoints, no 429).

- [ ] **Step 3: Apply the policies**
  - `.RequireAuthorization(AuthPolicies.Admin)` on the whole groups in `DataReadServerEndpoints`, `DatabaseEndpoints` and `CompanyEndpoints`.
  - In `ProductCompanyEndpoints`, only on refresh-settings GET/PUT, `discover`, `tracked` GET/PUT.
  - Dashboards, `statistics` and `update-data` stay on the fallback policy.

- [ ] **Step 4: Forwarded headers + rate limiter**
  - `UseForwardedHeaders` with `ForwardedHeaders.XForwardedFor` (default loopback trust), called before authentication.
  - `AddRateLimiter`: fixed window of 1 minute, partitioned by `HttpContext.Connection.RemoteIpAddress`, permit = `LoginRateLimitPerMinute`, `RejectionStatusCode = 429`.
  - `UseRateLimiter()`.

- [ ] **Step 5: Localhost and Swagger**
  - `Api/appsettings.json`: `"Urls": "http://localhost:5184"`; remove the `"Swagger"` section.
  - `Program.cs`: Swagger only under `app.Environment.IsDevelopment()`.

- [ ] **Step 6: Run all tests**

Run: `dotnet test tests/KrosExtension.Api.Tests`
Expected: all pass.

- [ ] **Step 7: Commit**

```bash
git add src/KrosExtension.Api tests/KrosExtension.Api.Tests
git commit -m "feat: admin-only settings endpoints, localhost Api, login rate limit"
```

---

### Task 6: Frontend login, forced password change, role-based menu

**Files:**
- Create: `src/KrosExtension.Web/ClientApp/src/api.ts`, `src/KrosExtension.Web/ClientApp/src/auth.tsx`
- Modify: `src/KrosExtension.Web/ClientApp/src/main.tsx`, `src/KrosExtension.Web/ClientApp/src/styles.css`

**Interfaces:**
- Consumes: `/api/auth/*` (Task 3).
- Produces:
  - `api.ts`: `export async function apiFetch(input: RequestInfo, init?: RequestInit): Promise<Response>`, which calls `fetch` and on status 401 dispatches `window.dispatchEvent(new Event('auth:unauthorized'))` before returning the response. Also `export function onUnauthorized(handler: () => void): () => void` (subscribe/unsubscribe).
  - `auth.tsx`:
    - `export type CurrentUser = { id: string; userName: string; role: 'Admin' | 'User'; mustChangePassword: boolean }`
    - `export function AuthGate({ children }: { children: (user: CurrentUser, logout: () => Promise<void>) => React.ReactNode })`
    - `export function ChangePasswordForm({ onDone, onCancel }: { onDone: (user: CurrentUser) => void; onCancel?: () => void })`
  - `App` becomes `function App({ user, onLogout }: { user: CurrentUser; onLogout: () => Promise<void> })`.

- [ ] **Step 1: Write `api.ts`.** Replace every `fetch(` call in `main.tsx` with `apiFetch(` (about 13 call sites) and import it.

- [ ] **Step 2: Write `AuthGate`**
  - On mount it calls `GET /api/auth/me`.
  - 401 → login form (copy from Global Constraints; the error message comes from the response text).
  - `mustChangePassword` → `ChangePasswordForm` without cancel.
  - Otherwise → `children(user, logout)`.
  - Subscribes to `onUnauthorized` → resets to the login form.
  - The login form uses the existing dark sidebar color `#202733` as a full-page background with a centered white card, matching the approved mockup.

- [ ] **Step 3: Write `ChangePasswordForm`**
  - Fields: current, new and new again (labels `Súčasné heslo`, `Nové heslo`, `Nové heslo znova`).
  - Client-side checks with the same rules as the server: min 10 chars, new ≠ current, the two new fields equal (`Heslá sa nezhodujú.`).
  - The server's 400 text is shown as-is.

- [ ] **Step 4: Update `App` in `main.tsx`**
  - `Section` gains `'admin-users'`.
  - The `Settings ▾` submenu (Servery / Olymp / Omega) renders only when `user.role === 'Admin'`.
  - A new `Admin ▾` submenu with `Používatelia` renders only for Admin. It shows a placeholder until Task 7.
  - The sidebar bottom, above the copyright, gets a user block: user name, `Zmeniť heslo` (opens `ChangePasswordForm` in the existing `HelpModal`-style overlay with cancel) and `Odhlásiť`.
  - `createRoot(...).render(<AuthGate>{(user, logout) => <App user={user} onLogout={logout} />}</AuthGate>)`.

- [ ] **Step 5: Build**

Run: `cd src/KrosExtension.Web/ClientApp && npm run build`
Expected: `✓ built`, no TypeScript errors.

- [ ] **Step 6: Manual check.** Run Api (Development) and `npm run dev`, then:
  1. Not logged in → login page.
  2. Wrong password → `Nesprávne meno alebo heslo`.
  3. Bootstrap admin → forced change screen → app with Settings + Admin menus.
  4. A User (create via Swagger or Task 7) sees only Olymp/Omega.
  5. Logout → login page.

- [ ] **Step 7: Commit**

```bash
git add src/KrosExtension.Web/ClientApp/src
git commit -m "feat: login page, forced password change and role-based menu"
```

---

### Task 7: Admin → Users page

**Files:**
- Create: `src/KrosExtension.Web/ClientApp/src/admin/UsersPage.tsx`
- Modify: `src/KrosExtension.Web/ClientApp/src/main.tsx` (render `UsersPage` for `'admin-users'`), `src/KrosExtension.Web/ClientApp/src/styles.css`

**Interfaces:**
- Consumes: `/api/admin/users*` (Task 4), `apiFetch`, `CurrentUser`.
- Produces: `export function UsersPage({ currentUserId }: { currentUserId: string })`.

- [ ] **Step 1: Implement `UsersPage`** using the existing classes `page`, `server-form`, `companies-table` and `status`.
  - Columns: `Meno`, `Rola`, `Stav`, `Posledné prihlásenie`, actions.
  - Status labels: `aktívny`, `zablokovaný`, `musí zmeniť heslo`, `zamknutý` (when `isLockedOut`).
  - Header button `+ Nový` opens an inline form with `Meno` and `Rola` (select `Používateľ` / `Admin`).
  - Row actions: `Reset hesla`, `Zablokovať` / `Odblokovať`, role select. Block and role controls are hidden on the current user's own row.
  - After create or reset, a modal shows the temporary password with a `Kopírovať` button (`navigator.clipboard.writeText`) and the text `Dočasné heslo sa už znova nezobrazí. Odovzdaj ho používateľovi, pri prvom prihlásení si ho zmení.`
  - Server error texts are shown in `.status.error`.

- [ ] **Step 2: Build**

Run: `cd src/KrosExtension.Web/ClientApp && npm run build`
Expected: `✓ built`.

- [ ] **Step 3: Manual check.**
  1. Create a user and log in as them in a private window with the temp password → forced change works.
  2. As admin: reset their password → the private window is logged out on its next action.
  3. Block → the same.
  4. Own row has no block/role controls.

- [ ] **Step 4: Commit**

```bash
git add src/KrosExtension.Web/ClientApp/src
git commit -m "feat: admin users page"
```

---

### Task 8: HTTPS on the Web host, client IP forwarding, deployment

**Files:**
- Modify: `src/KrosExtension.Web/Program.cs`
- Modify: `scripts/Install-Service.ps1`, `readme_deploy.md`

**Interfaces:**
- Consumes: Api `UseForwardedHeaders` (Task 5).
- Produces: production config files `C:\KrosExtension\web\appsettings.Production.json` and `C:\KrosExtension\api\appsettings.Production.json`, which the install script creates only when missing and never overwrites.

- [ ] **Step 1: Web `Program.cs`**
  - In `ProxyToApiAsync`, skip incoming `X-Forwarded-For` / `X-Forwarded-Proto` / `X-Forwarded-Host` headers so clients cannot spoof them.
  - Set `X-Forwarded-For` to `context.Connection.RemoteIpAddress`.
  - Forward multiple `Set-Cookie` values intact. The existing `ToArray()` copy already does; verify in Step 4.
  - `if (!app.Environment.IsDevelopment()) app.UseHsts();`

- [ ] **Step 2: `Install-Service.ps1`**
  - New optional parameters: `-HostName` (default `$env:COMPUTERNAME`), `-CertificatePath`, `-CertificatePassword`, `-CertificateSubject`, `-BootstrapAdminPassword`.
  - After copying, if `web\appsettings.Production.json` is missing, write it with `Kestrel:Endpoints:Https:Url = "https://0.0.0.0:5284"`. The certificate is either `{ "Path": ..., "Password": ... }` when `-CertificatePath` is given, or a Windows store lookup `{ "Subject": <CertificateSubject>, "Store": "My", "Location": "LocalMachine" }` when `-CertificateSubject` is given (Kestrel config cannot select by thumbprint). If neither is given, warn that HTTPS will not start until the file is completed.
  - If `api\appsettings.Production.json` is missing, write `Bootstrap` with `AdminUserName = "admin"` and `AdminPassword` = `-BootstrapAdminPassword` (or empty with a warning).
  - Final message: `Web UI: https://<HostName>:5284`.
  - Ensure `dist` never contains `appsettings.Production.json` (do not add such a file to the projects).

- [ ] **Step 3: `readme_deploy.md`.** Add a section `Prihlásenie a HTTPS` (Slovak) covering:
  - DNS record on the internal DNS server.
  - Firewall: allow 5284 only from the LAN; 5184 must not be opened.
  - Certificate, either from a company CA, or self-signed: `New-SelfSignedCertificate -DnsName ledgerpulse.firma.local -CertStoreLocation Cert:\LocalMachine\My`, export the public part and import it into `Trusted Root` on client PCs, e.g. via GPO.
  - First login with the bootstrap password, then clearing `AdminPassword` from `api\appsettings.Production.json`.
  - Emergency admin recovery (block the admin in the DB or set the password and restart).
  - **Backup of `C:\ProgramData\KrosExtension\keys`**: without it, sessions are lost (minor) and encrypted SQL passwords cannot be decrypted (Task 9).

- [ ] **Step 4: Manual verification**
  1. `dotnet dev-certs https --export-path %TEMP%\lp.pfx -p test1234`.
  2. Run Web with `ASPNETCORE_ENVIRONMENT=Production` and the Kestrel HTTPS endpoint pointed at that PFX via env vars (`Kestrel__Endpoints__Https__Url`, `...__Certificate__Path`, `...__Certificate__Password`).
  3. Run Api in Development on localhost.
  4. `curl -k -i https://localhost:5284/api/auth/me` → 401.
  5. Log in via browser → `Set-Cookie` present with `secure; samesite=strict; httponly`.
  6. Restart the Api process → the same browser session still works (persisted keys).

- [ ] **Step 5: Commit**

```bash
git add src/KrosExtension.Web/Program.cs scripts/Install-Service.ps1 readme_deploy.md
git commit -m "feat: HTTPS web host, client IP forwarding and deployment docs"
```

---

### Task 9: Encrypt SQL passwords of data read servers

**Files:**
- Create: `src/KrosExtension.Infrastructure/Persistence/SecretValueConverter.cs`
- Create: `src/KrosExtension.Application/Configuration/IDataReadServerSecretMigrator.cs`, `src/KrosExtension.Infrastructure/Configuration/DataReadServerSecretMigrator.cs`
- Modify: `ApplicationDbContext.cs`, `Infrastructure.csproj` (add `Microsoft.AspNetCore.DataProtection.Abstractions`), `Infrastructure/DependencyInjection.cs`, `Api/Program.cs`, `Directory.Packages.props`
- Test: `tests/KrosExtension.Api.Tests/SecretEncryptionTests.cs`

**Interfaces:**
- Consumes: Data Protection registered in the Api (Task 3).
- Produces:
  - `ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDataProtectionProvider? dataProtectionProvider = null)`. The design-time factory keeps passing only options.
  - `sealed class SecretValueConverter(IDataProtector protector) : ValueConverter<string, string>`. Stored format: `"enc:" + protector.Protect(value)`. Empty strings are stored as empty. Values without the `enc:` prefix are read as plaintext (legacy rows). Purpose string: `"KrosExtension.DataReadServerSecrets"`. Use static helper methods, because expression trees can't hold statement lambdas.
  - Applied in `OnModelCreating` to `DataReadServer.OlympPassword` and `OmegaPassword` only when the provider is not null.
  - `interface IDataReadServerSecretMigrator { Task<int> EncryptPlaintextSecretsAsync(CancellationToken ct = default); }` → number of servers re-saved.

- [ ] **Step 1: Write the failing tests**

```csharp
AddServer_StoresPasswordsEncrypted()
  // admin POST /api/data-read-servers with SqlLogin Olymp credentials; raw SQL:
  // SELECT OlympPassword AS Value FROM DataReadServers → starts with "enc:" and does not contain the password
EncryptedPassword_RoundTripsThroughEntity()
  // new scope → dbContext.DataReadServers.Single().OlympPassword == original
Migrator_EncryptsLegacyPlaintextRows()
  // INSERT a row via raw SQL with plaintext OmegaPassword "legacy-secret" → EncryptPlaintextSecretsAsync() == 1
  // → raw value starts with "enc:", entity value == "legacy-secret"; a second run returns 0
```

For the raw INSERT, take the column list from `DataReadServerConfiguration` and the latest migration snapshot. Do not invent columns.

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `dotnet test tests/KrosExtension.Api.Tests --filter SecretEncryptionTests`
Expected: FAIL (raw value is plaintext).

- [ ] **Step 3: Implement the converter and the context change.** Column length 512 is enough for passwords up to about 250 characters. Keep it as is.

- [ ] **Step 4: Implement `DataReadServerSecretMigrator`**
  - Select ids with `Database.SqlQuery<Guid>($"SELECT Id AS Value FROM DataReadServers WHERE (OlympPassword <> '' AND OlympPassword NOT LIKE 'enc:%') OR (OmegaPassword <> '' AND OmegaPassword NOT LIKE 'enc:%')")`.
  - Load those entities, mark both password properties modified, save.
  - Register it, and call it in `Program.cs` startup right after the bootstrapper. Log the count, never the values.

- [ ] **Step 5: Run all tests**

Run: `dotnet test tests/KrosExtension.Api.Tests`
Expected: all pass.

- [ ] **Step 6: Manual check.** Against the dev LocalDB, start the Api and confirm the existing server rows now start with `enc:`. Then confirm that the Olymp/Omega dashboards still load data, which proves `DataReadServerConnectionStringFactory` receives decrypted values.

- [ ] **Step 7: Commit**

```bash
git add Directory.Packages.props src/KrosExtension.Application/Configuration src/KrosExtension.Infrastructure src/KrosExtension.Api/Program.cs tests/KrosExtension.Api.Tests
git commit -m "feat: encrypt data read server SQL passwords at rest"
```
