# FreeA11yChecker.DataAccess

Server-side data access layer for FreeA11yChecker. Implements `IDataAccess` as a large partial class covering all database I/O, business logic, authentication, PDF report generation, Azure AD / LDAP integration, and Microsoft Graph calls. Consumed exclusively by the `FreeA11yChecker` web host.

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net10.0` |
| Output type | Class library |

## What It Does

- All EF Core queries go through this layer. Database migrations for SQL Server, SQLite, PostgreSQL, and MySQL are each in their own partial class (`DataMigrations.*.cs`).
- Scan data operations: save scan runs, page results, violations, screenshots, images, certificates, ranked rules, and artifacts; prune old scans; return pending runs for the background service.
- Site and page management: CRUD for `Site`, `SitePage`, and `SiteCredential` records with tenant isolation.
- Audit report generation: `GenerateAuditReport` builds a full WCAG criterion pass/fail matrix from stored scan data; `ExportAuditCsv` produces a CSV export; PDF rendering via QuestPDF.
- User management: create, read, update, delete, and look up users by username, email, employee ID, or GUID; handle password resets, account lockout, token generation and validation.
- Multi-tenant: all queries are scoped to a `TenantId`; tenant CRUD is in `DataAccess.Tenants.cs`.
- Authentication: `DataAccess.Authenticate.cs` handles local-account login, JWT token issuance (`DataAccess.JWT.cs`), and delegates to `DataAccess.ActiveDirectory.cs` for LDAP/AD lookups.
- Encryption: AES-256 field-level encryption for sensitive settings (`DataAccess.Encryption.cs`).
- Plugin integration: `DataAccess.Plugins.cs` invokes loaded plugins at the appropriate lifecycle points.
- File storage: binary file upload/download with tenant isolation (`DataAccess.FileStorage.cs`).
- MS Graph: `GraphAPI.cs` fetches user profile and group data from Microsoft Graph.

## Key Classes / Methods (Scan-Specific)

| Interface method | Purpose |
|---|---|
| `GetScanRuns(ids, siteId, tenantId)` | Returns scan run history, ordered by start time descending, with denormalized site names |
| `GetPendingScanRuns()` | Returns all runs with `Status = "Queued"` — polled by the background service |
| `SaveScanRun(item)` | Upserts a scan run record |
| `SavePageScanResult(item)` | Upserts a page-level scan result |
| `SaveViolations(items)` | Bulk-inserts `A11yViolation` records |
| `GetViolationsByRule(scanRunId, canonicalRuleId)` | Returns all violations for a specific rule within a scan run |
| `GetCrossSiteViolations(tenantId, filter)` | Cross-site violation aggregation used by the rule hotlist and search pages |
| `SaveViolationSuppression` / `DeleteViolationSuppression` | Manage per-tenant violation suppressions |
| `PruneOldScans(siteId, keepCount)` | Deletes scan runs beyond the keep count, cascading to all child records |
| `GenerateAuditReport(siteId)` | Builds a `DataObjects.AuditReport` with WCAG criterion pass/fail status from stored violations |

## Key Classes / Methods (User-Specific)

| Interface method | Purpose |
|---|---|
| `GetUser(userId)` | Load a single user by ID with all profile data |
| `GetUserByUsernameOrEmail(tenantId, search)` | Primary login lookup; optionally creates the user if not found |
| `SaveUser(user)` | Upsert a user record, fires `UserUpdate` plugins |
| `GetUserFromToken(token, fingerprint)` | Validates a JWT and returns the associated user |
| `GetUserToken(tenantId, userId)` | Issues a signed JWT for a user |
| `GetUsersFiltered(filter)` | Paginated, filtered user list for the admin UI |

## Project References

| Project | Role |
|---|---|
| `FreeA11yChecker.DataObjects` | DTOs for all input and output types |
| `FreeA11yChecker.EFModels` | EF Core entities and `DbContext` |
| `FreeA11yChecker.Plugins` | Plugin host for lifecycle hooks |

## Notable NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.Graph` | Azure AD / Microsoft 365 user data |
| `Novell.Directory.Ldap.NETStandard` | LDAP / Active Directory authentication |
| `QuestPDF` | PDF audit report rendering |
| `JWTHelpers` | JWT token issuance and validation |
| `Azure.Identity` | Azure credential chain for Graph API |
| `Brad.Wickett_Sql2LINQ` | Dynamic LINQ query helpers |
| `CsvHelper` | CSV export |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
