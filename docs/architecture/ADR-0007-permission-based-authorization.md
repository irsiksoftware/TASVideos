# ADR-0007: Permission-Based Authorization with ASP.NET Core Identity

## Status

Accepted

## Date

2024-11-18 (Documented retrospectively)

## Decision Makers

* TASVideos Development Team

## Context

TASVideos has complex authorization requirements:
- **Granular permissions:** Different actions require different permissions (judge submissions, edit wiki, moderate forums, etc.)
- **Role composition:** Users can have multiple roles (e.g., Judge + Publisher)
- **Permission groups:** Permissions logically grouped by area (wiki, forums, submissions, admin)
- **Dual authentication:** Cookie-based for web UI, JWT tokens for API
- **Security:** Strong password requirements, email confirmation, secure password hashing
- **User management:** Admins need to assign roles and permissions
- **Audit trail:** Track who has which permissions and when granted

Authorization examples:
- Only Judges can judge submissions
- Only Publishers can publish movies
- Only Admins can edit system wiki pages
- Anyone with "CreateForumPosts" can create forum posts
- Users can edit their own profile, but only Admins can edit others' profiles

Traditional role-based authorization (RBAC) has limitations:
- Roles like "Moderator" are too coarse-grained
- Hard to compose roles (e.g., Judge + Publisher)
- Adding new permissions requires new roles or modifying existing ones

Permission-based authorization is more flexible:
- Fine-grained control
- Composable (users can have multiple roles, each with multiple permissions)
- Easy to add new permissions without role changes

## Decision

Implement **permission-based authorization** built on **ASP.NET Core Identity** with:
- Permission enum for compile-time safety
- Many-to-many: Users -> Roles -> Permissions
- Claims-based authorization for performance
- Dual authentication: Cookies (web) + JWT (API)
- Strong password requirements following OWASP 2023 guidelines

### Permission Model

**Permission Enum:** TASVideos.Data/Entity/PermissionTo.cs

```csharp
public enum PermissionTo
{
    // User permissions (1-99)
    CreateForumPosts = 1,
    EditHomePage = 2,
    SubmitMovies = 3,
    RateMovies = 4,
    VoteOnPolls = 5,
    SendPrivateMessages = 6,
    // ...

    // Wiki permissions (100-199)
    EditWikiPages = 100,
    EditGameResources = 101,
    EditSystemPages = 102,
    EditRoles = 103,
    CreateNewWikiPages = 104,
    DeleteWikiPages = 105,
    // ...

    // Queue Maintenance (200-299)
    JudgeSubmissions = 200,
    PublishMovies = 201,
    EditSubmissions = 202,
    CancelSubmissions = 203,
    // ...

    // Publication Maintenance (300-399)
    SetPublicationClass = 300,
    CatalogMovies = 301,
    EditPublicationMetadata = 302,
    ObsoleteMovies = 303,
    // ...

    // Forum Moderation (400-499)
    EditUsersForumPosts = 400,
    DeleteForumPosts = 401,
    CreateForumCategory = 402,
    LockTopics = 403,
    // ...

    // User Administration (500-599)
    DeleteRoles = 500,
    EditUsers = 502,
    AssignRoles = 504,
    ViewPrivateMessages = 505,
    // ...

    // Admin (9000+)
    SeeDiagnostics = 9001,
    SeeEmails = 9002,
    AccessAdminDashboard = 9003,
}
```

**Design Rationale:**
- Numeric ranges group related permissions (1xx for wiki, 2xx for submissions, etc.)
- Enum provides compile-time safety (no typos)
- Gaps allow future expansion within each category
- Descriptive names make intent clear

### Database Schema

**Entity Relationships:**
```
User (AspNetUsers)
  └─ UserRole (many-to-many join table)
      └─ Role (AspNetRoles)
          └─ RolePermission (many-to-many join table)
              └─ Permission (PermissionTo enum)
```

**Key Tables:**
- `AspNetUsers` - User accounts (from Identity)
- `AspNetRoles` - Role definitions (e.g., "Judge", "Publisher")
- `AspNetUserRoles` - User-to-Role mapping
- `RolePermission` - Role-to-Permission mapping
- Permissions stored as integers (enum values)

### Authentication Setup

**ASP.NET Core Identity:** tasvideos/Extensions/ServiceCollectionExtensions.cs

```csharp
services.AddIdentity<User, Role>(config =>
{
    // Email confirmation
    config.SignIn.RequireConfirmedEmail = env.IsProduction() || env.IsStaging();

    // Password requirements (OWASP 2023)
    config.Password.RequiredLength = 12;
    config.Password.RequireDigit = false;
    config.Password.RequireLowercase = false;
    config.Password.RequireUppercase = false;
    config.Password.RequireNonAlphanumeric = false;
    config.Password.RequiredUniqueChars = 4;

    // Email
    config.User.RequireUniqueEmail = true;

    // Username characters (support international names)
    config.User.AllowedUserNameCharacters +=
        "āàâãáäéèëêíîïóôöúüûý£ŉçÑñ";
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Password hashing (OWASP 2023 recommendation)
services.Configure<PasswordHasherOptions>(options =>
    options.IterationCount = 720_000);
```

**Dual Authentication:**

1. **Cookie Authentication (Web UI)**
   - 90-day expiration
   - Sliding expiration (renewed on activity)
   - Persistent login option

2. **JWT Bearer (API)**
   - Stateless tokens
   - Short expiration (configurable)
   - Refresh token flow (optional)

**Configuration:** TASVideos.Api/ServiceCollectionExtensions.cs

```csharp
services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(90);
    options.SlidingExpiration = true;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = true;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});
```

### Authorization Usage

**Razor Pages:**
```csharp
[RequirePermission(PermissionTo.JudgeSubmissions)]
public class JudgeSubmissionPage : PageModel
{
    // Only users with JudgeSubmissions permission can access
}
```

**Service Layer:**
```csharp
public async Task<bool> CanUserJudge(int userId)
{
    return await _userManager.HasPermission(
        userId,
        PermissionTo.JudgeSubmissions);
}
```

**View Templates:**
```html
@if (User.Has(PermissionTo.EditWikiPages))
{
    <a href="/wiki/edit/@Model.PageName">Edit</a>
}
```

### Service Implementations

**User Management:** TASVideos.Core/Services/UserManager.cs

```csharp
public interface IUserManager
{
    Task<bool> HasPermission(int userId, PermissionTo permission);
    Task<IEnumerable<PermissionTo>> GetUserPermissions(int userId);
    Task<bool> IsInRole(int userId, string roleName);
}
```

**Role Management:** TASVideos.Core/Services/RoleService.cs

```csharp
public interface IRoleService
{
    Task<IEnumerable<Role>> GetAllRoles();
    Task<Role?> GetRole(int roleId);
    Task CreateRole(string name, string description);
    Task AddPermissionToRole(int roleId, PermissionTo permission);
    Task RemovePermissionFromRole(int roleId, PermissionTo permission);
}
```

**Claims-Based Caching:**

When user logs in, all permissions are loaded into claims:
```csharp
var permissions = await GetUserPermissions(user.Id);
var claims = permissions.Select(p =>
    new Claim("permission", ((int)p).ToString()));

await _signInManager.SignInWithClaimsAsync(user, claims);
```

This avoids database queries on every permission check.

## Alternatives Considered

### Role-Based Authorization Only
**Example:** Roles like "Moderator", "Admin", "Judge"

**Pros:**
- Simpler to understand
- Built into ASP.NET Core Identity

**Cons:**
- Too coarse-grained (what if someone is Judge but not Publisher?)
- Role explosion (need Judge, Publisher, JudgeAndPublisher, etc.)
- Hard to add new permissions without new roles

**Why not chosen:** Insufficient granularity for TASVideos' complex requirements.

### Policy-Based Authorization
**Example:**
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("CanJudge", policy =>
        policy.RequireClaim("permission", "200"));
});
```

**Pros:**
- Flexible
- Built into ASP.NET Core

**Cons:**
- Stringly-typed ("permission", "200")
- Hard to discover available policies
- Policies defined separately from usage

**Why not chosen:** Permission enum provides better compile-time safety.

### Attribute-Based Access Control (ABAC)
**Example:** Rules based on attributes (user.department == "moderation")

**Pros:**
- Very flexible
- Dynamic rules

**Cons:**
- Complex to implement
- Harder to reason about
- Overkill for TASVideos
- Performance overhead (rule evaluation)

**Why not chosen:** TASVideos has well-defined permissions, not dynamic rules.

### Third-Party Auth (Auth0, Okta)
**Pros:**
- Hosted solution
- Advanced features (SSO, MFA)
- Offload security responsibility

**Cons:**
- Ongoing costs
- External dependency
- Less control
- Integration complexity
- Data residency concerns

**Why not chosen:** ASP.NET Core Identity is sufficient and free.

### Custom Authentication System
**Example:** Build from scratch with hashed passwords

**Pros:**
- Full control
- No dependencies

**Cons:**
- Security risk (easy to get wrong)
- Maintenance burden
- Missing features (password reset, email confirmation, etc.)
- Reinventing the wheel

**Why not chosen:** ASP.NET Core Identity is battle-tested and secure.

## Consequences

### Positive

* **Granular control:** Fine-grained permissions for specific actions
* **Composable roles:** Users can have multiple roles with different permissions
* **Compile-time safety:** Enum prevents typos and provides IntelliSense
* **Organized permissions:** Numeric grouping makes permissions discoverable
* **Claims-based performance:** Permissions cached in claims (no DB queries)
* **Extensibility:** Easy to add new permissions by extending enum
* **Audit trail:** Database tracks role assignments and changes
* **Strong security:** OWASP-compliant password hashing and requirements
* **Dual authentication:** Supports both web and API clients
* **International support:** Usernames support non-ASCII characters

### Negative

* **Complexity:** More complex than simple role-based auth
* **Migration required:** Adding permissions requires database migration
* **Enum growth:** Permission enum will grow over time
* **Claims invalidation:** Permissions changes require re-login (mitigated: rare occurrence)
* **Learning curve:** Developers must understand permission model

### Neutral

* **Permission discovery:** Developers must check enum for available permissions
* **Role naming:** Role names are convention-based (no enforcement)
* **Permission assignment:** Admins must understand which permissions to grant

## Security Considerations

1. **Password Hashing:**
   - PBKDF2 with 720,000 iterations (OWASP 2023)
   - Automatic rehashing on login if iteration count changes

2. **Password Requirements:**
   - Minimum 12 characters
   - No complexity requirements (OWASP guideline)
   - At least 4 unique characters

3. **Email Confirmation:**
   - Required in production/staging
   - Prevents fake accounts

4. **JWT Security:**
   - HTTPS required
   - Short expiration
   - Symmetric key signing

5. **Cookie Security:**
   - HttpOnly flag
   - Secure flag (HTTPS only)
   - SameSite=Strict

## Future Considerations

1. **Two-Factor Authentication (2FA):** Add support for TOTP/authenticator apps
2. **OAuth Providers:** Allow login with GitHub, Google, Discord
3. **Permission history:** Track when permissions were granted/revoked
4. **Permission groups:** Group related permissions for bulk assignment
5. **Time-based permissions:** Temporary permission grants (e.g., "Judge for 30 days")

## Links

* Code: [PermissionTo.cs](../../TASVideos.Data/Entity/PermissionTo.cs)
* Code: [User.cs](../../TASVideos.Data/Entity/User.cs)
* Code: [Role.cs](../../TASVideos.Data/Entity/Role.cs)
* Code: [UserManager.cs](../../TASVideos.Core/Services/UserManager.cs)
* Code: [RoleService.cs](../../TASVideos.Core/Services/RoleService.cs)
* Code: [ServiceCollectionExtensions.cs](../../tasvideos/Extensions/ServiceCollectionExtensions.cs)
* Related ADRs: [ADR-0002](./ADR-0002-postgresql-database.md) - PostgreSQL Database
* Related ADRs: [ADR-0006](./ADR-0006-minimal-api-with-field-selection.md) - API Strategy
* Documentation: [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
* Documentation: [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
