using System.Security.Cryptography;
using System.Text;

namespace FreeServicesHub;

// ---- FreeServicesHub.App.DataAccess.cs ----
// FreeServicesHub data-access hook bodies, called from one line in each hook in DataAccess.App.cs.
// DataAccess.App.cs stays stock FreeCRM apart from those lines so a framework upgrade can overwrite it.

public partial class DataAccess
{
    /// <summary>
    /// Removes FreeServicesHub records that belong to an agent or a tenant before the record itself is deleted.
    /// Called first in DeleteRecordsApp; a false Result stops the delete with the messages returned here.
    /// </summary>
    /// <param name="Rec">The EF record object.</param>
    /// <param name="CurrentUser">The User object for the current user, if loaded.</param>
    /// <returns>A BooleanResponse object.</returns>
    private async Task<DataObjects.BooleanResponse> FreeServicesHubDeleteRecordsApp(object Rec, DataObjects.User? CurrentUser = null)
    {
        var output = new DataObjects.BooleanResponse();

        try {
            if (Rec is EFModels.EFModels.Agent) {
                var rec = Rec as EFModels.EFModels.Agent;
                if (rec != null) {
                    // Cascade delete heartbeats and API tokens for this agent
                    data.AgentHeartbeats.RemoveRange(data.AgentHeartbeats.Where(h => h.AgentId == rec.AgentId));
                    data.ApiClientTokens.RemoveRange(data.ApiClientTokens.Where(t => t.AgentId == rec.AgentId));
                    await data.SaveChangesAsync();
                }
            }

            if (Rec is EFModels.EFModels.Tenant) {
                var rec = Rec as EFModels.EFModels.Tenant;
                if (rec != null) {
                    // Cascade delete all agent-related records for this tenant
                    var agentIds = data.Agents.Where(a => a.TenantId == rec.TenantId).Select(a => a.AgentId).ToList();
                    if (agentIds.Count > 0) {
                        data.AgentHeartbeats.RemoveRange(data.AgentHeartbeats.Where(h => agentIds.Contains(h.AgentId)));
                        data.ApiClientTokens.RemoveRange(data.ApiClientTokens.Where(t => agentIds.Contains(t.AgentId)));
                        data.RegistrationKeys.RemoveRange(data.RegistrationKeys.Where(k => k.TenantId == rec.TenantId));
                        data.Agents.RemoveRange(data.Agents.Where(a => a.TenantId == rec.TenantId));
                        await data.SaveChangesAsync();
                    }
                }
            }
        } catch (Exception ex) {
            output.Messages.Add("An Error Occurred in DeleteUserApp");
            output.Messages.AddRange(RecurseException(ex));
        }

        output.Result = output.Messages.Count == 0;

        return output;
    }

    /// <summary>
    /// Loads the current tenant's agents into the Blazor data model for the dashboard. Called from GetBlazorDataModelApp.
    /// </summary>
    /// <param name="blazorDataModelLoader">The loader being built.</param>
    /// <param name="CurrentUser">The User object for the current user, if loaded.</param>
    /// <returns>The loader with AgentStatuses set.</returns>
    private async Task<DataObjects.BlazorDataModelLoader> FreeServicesHubGetBlazorDataModelApp(DataObjects.BlazorDataModelLoader blazorDataModelLoader, DataObjects.User? CurrentUser = null)
    {
        var output = blazorDataModelLoader;

        // Load agent summary data for the dashboard
        Guid tenantId = CurrentUser?.TenantId ?? Guid.Empty;
        if (tenantId != Guid.Empty) {
            List<DataObjects.Agent> agents = await data.Agents
                .Where(a => a.TenantId == tenantId && !a.Deleted)
                .Select(a => new DataObjects.Agent {
                    AgentId = a.AgentId,
                    TenantId = a.TenantId,
                    Name = a.Name ?? "",
                    Hostname = a.Hostname ?? "",
                    OperatingSystem = a.OperatingSystem ?? "",
                    Architecture = a.Architecture ?? "",
                    AgentVersion = a.AgentVersion ?? "",
                    DotNetVersion = a.DotNetVersion ?? "",
                    Status = a.Status ?? DataObjects.AgentStatuses.Offline,
                    LastHeartbeat = a.LastHeartbeat,
                    RegisteredAt = a.RegisteredAt,
                    RegisteredBy = a.RegisteredBy ?? "",
                    Added = a.Added,
                    AddedBy = a.AddedBy ?? "",
                    LastModified = a.LastModified,
                    LastModifiedBy = a.LastModifiedBy ?? "",
                    Deleted = a.Deleted,
                    DeletedAt = a.DeletedAt,
                })
                .OrderBy(a => a.Name)
                .ToListAsync();

            output.AgentStatuses = agents;
        }

        return output;
    }

    /// <summary>
    /// FreeServicesHub background work. Called from ProcessBackgroundTasksApp. Errors are added to Messages while
    /// Result stays true, as before the move.
    /// </summary>
    /// <param name="TenantId">The tenant id being processed.</param>
    /// <param name="Iteration">The background processor's iteration counter.</param>
    /// <returns>A BooleanResponse object.</returns>
    private async Task<DataObjects.BooleanResponse> FreeServicesHubProcessBackgroundTasksApp(Guid TenantId, long Iteration)
    {
        var output = new DataObjects.BooleanResponse();
        output.Result = true;

        // Prune old heartbeat records every 5 minutes (assuming 10-second processing interval, 30 iterations = 5 min)
        if (Iteration % 30 == 0) {
            try {
                int retentionHours = ConfigurationHelper != null ? ConfigurationHelper.HeartbeatRetentionHours : 24;
                DateTime cutoff = DateTime.UtcNow.AddHours(-retentionHours);
                var staleHeartbeats = data.AgentHeartbeats.Where(h => h.Timestamp < cutoff);
                if (staleHeartbeats.Any()) {
                    data.AgentHeartbeats.RemoveRange(staleHeartbeats);
                    await data.SaveChangesAsync();
                }
            } catch (Exception ex) {
                output.Messages.Add("Error pruning old heartbeats: " + RecurseExceptionAsString(ex));
            }
        }

        return output;
    }
}

// ---- FreeServicesHub.App.DataAccess.Agents.cs ----
public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> DeleteAgents(List<Guid>? Ids, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.Agent>> GetAgents(List<Guid>? Ids, Guid TenantId, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.Agent>> SaveAgents(List<DataObjects.Agent> Items, DataObjects.User? CurrentUser = null);
}

public partial class DataAccess
{
    public async Task<DataObjects.BooleanResponse> DeleteAgents(List<Guid>? Ids, DataObjects.User? CurrentUser = null)
    {
        DataObjects.BooleanResponse output = new();

        if (Ids == null || !Ids.Any()) {
            output.Messages.Add("No Agent Ids provided for deletion.");
            return output;
        }

        DateTime now = DateTime.UtcNow;

        try {
            List<EFModels.EFModels.Agent> recs = await data.Agents.Where(x => Ids.Contains(x.AgentId)).ToListAsync();

            if (!recs.Any()) {
                output.Messages.Add("No matching Agent records found.");
                return output;
            }

            foreach (EFModels.EFModels.Agent rec in recs) {
                rec.Deleted = true;
                rec.DeletedAt = now;
                rec.LastModified = now;

                if (CurrentUser != null) {
                    rec.LastModifiedBy = CurrentUserIdString(CurrentUser);
                }
            }

            // Cascade soft-delete: mark any jobs assigned to these agents as Cancelled
            var agentIds = recs.Select(r => r.AgentId).ToList();
            var orphanedJobs = await data.HubJobs
                .Where(j => j.AgentId.HasValue && agentIds.Contains(j.AgentId.Value) && !j.Deleted
                    && j.Status != "Completed" && j.Status != "Failed" && j.Status != "Cancelled")
                .ToListAsync();
            foreach (var job in orphanedJobs) {
                job.Status = "Cancelled";
                job.ErrorMessage = "Agent deleted";
                job.CompletedAt = now;
                job.LastModified = now;
            }

            await data.SaveChangesAsync();
            output.Result = true;

            foreach (EFModels.EFModels.Agent rec in recs) {
                await SignalRUpdate(new DataObjects.SignalRUpdate {
                    TenantId = rec.TenantId,
                    ItemId = rec.AgentId,
                    UpdateType = DataObjects.SignalRUpdateType.AgentStatusChanged,
                    Message = "Deleted",
                    UserId = CurrentUserId(CurrentUser),
                });
            }
        } catch (Exception ex) {
            output.Messages.Add("Error Deleting Agents");
            output.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }
    public async Task<List<DataObjects.Agent>> GetAgents(List<Guid>? Ids, Guid TenantId, DataObjects.User? CurrentUser = null)
    {
        List<DataObjects.Agent> output = new();

        List<EFModels.EFModels.Agent>? recs = null;

        if (Ids != null && Ids.Any()) {
            if (AdminUser(CurrentUser)) {
                recs = await data.Agents.Where(x => x.TenantId == TenantId && Ids.Contains(x.AgentId)).ToListAsync();
            } else {
                recs = await data.Agents.Where(x => x.TenantId == TenantId && Ids.Contains(x.AgentId) && x.Deleted != true).ToListAsync();
            }
        } else {
            if (AdminUser(CurrentUser)) {
                recs = await data.Agents.Where(x => x.TenantId == TenantId).ToListAsync();
            } else {
                recs = await data.Agents.Where(x => x.TenantId == TenantId && x.Deleted != true).ToListAsync();
            }
        }

        if (recs != null && recs.Any()) {
            foreach (EFModels.EFModels.Agent rec in recs) {
                output.Add(new DataObjects.Agent {
                    ActionResponse = GetNewActionResponse(true),
                    AgentId = rec.AgentId,
                    TenantId = rec.TenantId,
                    Name = rec.Name,
                    Hostname = rec.Hostname ?? string.Empty,
                    OperatingSystem = rec.OperatingSystem ?? string.Empty,
                    Architecture = rec.Architecture ?? string.Empty,
                    AgentVersion = rec.AgentVersion ?? string.Empty,
                    DotNetVersion = rec.DotNetVersion ?? string.Empty,
                    Status = rec.Status ?? string.Empty,
                    LastHeartbeat = rec.LastHeartbeat,
                    RegisteredAt = rec.RegisteredAt,
                    RegisteredBy = rec.RegisteredBy ?? string.Empty,
                    Added = rec.Added,
                    AddedBy = LastModifiedDisplayName(rec.AddedBy),
                    LastModified = rec.LastModified,
                    LastModifiedBy = LastModifiedDisplayName(rec.LastModifiedBy),
                    Deleted = rec.Deleted,
                    DeletedAt = rec.DeletedAt,
                });
            }
        }

        return output;
    }

    private async Task<DataObjects.Agent> SaveAgent(DataObjects.Agent agent, DataObjects.User? CurrentUser = null)
    {
        DataObjects.Agent output = agent;
        output.ActionResponse = GetNewActionResponse();

        bool newRecord = false;
        DateTime now = DateTime.UtcNow;

        EFModels.EFModels.Agent? rec = await data.Agents.FirstOrDefaultAsync(x => x.AgentId == output.AgentId);

        if (rec != null && rec.Deleted) {
            if (AdminUser(CurrentUser)) {
                // Ok to edit this record that is marked as deleted.
            } else {
                output.ActionResponse.Messages.Add("Agent '" + output.AgentId.ToString() + "' No Longer Exists");
                return output;
            }
        }

        if (rec == null) {
            if (output.AgentId == Guid.Empty) {
                newRecord = true;
                output.AgentId = Guid.NewGuid();

                rec = new EFModels.EFModels.Agent {
                    AgentId = output.AgentId,
                    TenantId = output.TenantId,
                    Deleted = false,
                    Added = now,
                    AddedBy = CurrentUserIdString(CurrentUser),
                };
            } else {
                output.ActionResponse.Messages.Add("Agent '" + output.AgentId.ToString() + "' No Longer Exists");
                return output;
            }
        }

        output.Name = MaxStringLength(output.Name, 255);
        output.Hostname = MaxStringLength(output.Hostname, 255);

        rec.Name = output.Name;
        rec.Hostname = output.Hostname;
        rec.OperatingSystem = output.OperatingSystem;
        rec.Architecture = output.Architecture;
        rec.AgentVersion = output.AgentVersion;
        rec.DotNetVersion = output.DotNetVersion;
        rec.Status = output.Status;
        rec.LastHeartbeat = output.LastHeartbeat;
        rec.RegisteredAt = output.RegisteredAt;
        rec.RegisteredBy = output.RegisteredBy;
        rec.LastModified = now;
        rec.LastModifiedBy = CurrentUserIdString(CurrentUser);

        if (AdminUser(CurrentUser)) {
            rec.Deleted = output.Deleted;

            if (!output.Deleted) {
                rec.DeletedAt = null;
            }
        }

        try {
            if (newRecord) {
                await data.Agents.AddAsync(rec);
            }
            await data.SaveChangesAsync();

            output.ActionResponse.Result = true;

            await SignalRUpdate(new DataObjects.SignalRUpdate {
                TenantId = output.TenantId,
                ItemId = output.AgentId,
                UpdateType = DataObjects.SignalRUpdateType.AgentStatusChanged,
                Message = "Saved",
                UserId = CurrentUserId(CurrentUser),
                Object = output,
            });
        } catch (Exception ex) {
            output.ActionResponse.Messages.Add("Error Saving Agent " + output.AgentId.ToString());
            output.ActionResponse.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.Agent>> SaveAgents(List<DataObjects.Agent> Items, DataObjects.User? CurrentUser = null)
    {
        List<DataObjects.Agent> output = new();

        foreach (DataObjects.Agent item in Items) {
            DataObjects.Agent saved = await SaveAgent(item, CurrentUser);
            output.Add(saved);
        }

        return output;
    }
}

// ---- FreeServicesHub.App.DataAccess.ApiKeys.cs ----
public partial interface IDataAccess
{
    Task<DataObjects.ApiClientToken> GenerateApiClientToken(Guid AgentId, Guid TenantId);
    Task<List<DataObjects.RegistrationKey>> GenerateRegistrationKeys(int Count, Guid TenantId, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.ApiClientToken>> GetApiClientTokens(Guid TenantId);
    Task<List<DataObjects.RegistrationKey>> GetRegistrationKeys(Guid TenantId);
    Task<DataObjects.BooleanResponse> RevokeApiClientToken(Guid TokenId, DataObjects.User? CurrentUser = null);
    Task<DataObjects.ApiClientToken?> ValidateApiClientToken(string PlaintextToken);
    Task<DataObjects.RegistrationKey?> ValidateRegistrationKey(string PlaintextKey, Guid TenantId);
}

public partial class DataAccess
{
    public async Task<DataObjects.ApiClientToken> GenerateApiClientToken(Guid AgentId, Guid TenantId)
    {
        DataObjects.ApiClientToken output = new();

        DateTime now = DateTime.UtcNow;

        try {
            string plaintext = GeneratePlaintextKey();
            string hash = HashKey(plaintext);
            string prefix = plaintext.Substring(0, 8);
            Guid tokenId = Guid.NewGuid();

            EFModels.EFModels.ApiClientToken rec = new() {
                ApiClientTokenId = tokenId,
                AgentId = AgentId,
                TenantId = TenantId,
                TokenHash = hash,
                TokenPrefix = prefix,
                Active = true,
                Created = now,
                RevokedAt = null,
                RevokedBy = null,
            };

            await data.ApiClientTokens.AddAsync(rec);
            await data.SaveChangesAsync();

            // Look up agent name for display
            EFModels.EFModels.Agent? agent = await data.Agents.FirstOrDefaultAsync(x => x.AgentId == AgentId);

            output.ActionResponse = GetNewActionResponse(true);
            output.ApiClientTokenId = tokenId;
            output.AgentId = AgentId;
            output.TenantId = TenantId;
            output.TokenHash = hash;
            output.TokenPrefix = prefix;
            output.Active = true;
            output.Created = now;
            output.AgentName = agent?.Name ?? string.Empty;
            output.NewTokenPlaintext = plaintext;
        } catch (Exception ex) {
            output.ActionResponse.Messages.Add("Error Generating API Client Token");
            output.ActionResponse.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }

    private static string GeneratePlaintextKey()
    {
        byte[] bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public async Task<List<DataObjects.RegistrationKey>> GenerateRegistrationKeys(int Count, Guid TenantId, DataObjects.User? CurrentUser = null)
    {
        List<DataObjects.RegistrationKey> output = new();

        DateTime now = DateTime.UtcNow;
        int expiryHours = ConfigurationHelper?.RegistrationKeyExpiryHours ?? 24;

        try {
            for (int i = 0; i < Count; i++) {
                string plaintext = GeneratePlaintextKey();
                string hash = HashKey(plaintext);
                string prefix = plaintext.Substring(0, 8);
                Guid keyId = Guid.NewGuid();

                EFModels.EFModels.RegistrationKey rec = new() {
                    RegistrationKeyId = keyId,
                    TenantId = TenantId,
                    KeyHash = hash,
                    KeyPrefix = prefix,
                    ExpiresAt = now.AddHours(expiryHours),
                    Used = false,
                    UsedByAgentId = null,
                    UsedAt = null,
                    Created = now,
                    CreatedBy = CurrentUserIdString(CurrentUser),
                };

                await data.RegistrationKeys.AddAsync(rec);
                await data.SaveChangesAsync();

                output.Add(new DataObjects.RegistrationKey {
                    ActionResponse = GetNewActionResponse(true),
                    RegistrationKeyId = keyId,
                    TenantId = TenantId,
                    KeyHash = hash,
                    KeyPrefix = prefix,
                    ExpiresAt = rec.ExpiresAt,
                    Used = false,
                    Created = now,
                    CreatedBy = LastModifiedDisplayName(rec.CreatedBy),
                    NewKeyPlaintext = plaintext,
                });
            }

            await SignalRUpdate(new DataObjects.SignalRUpdate {
                TenantId = TenantId,
                UpdateType = DataObjects.SignalRUpdateType.RegistrationKeyGenerated,
                Message = "Generated " + Count.ToString() + " registration key(s)",
                UserId = CurrentUserId(CurrentUser),
            });
        } catch (Exception ex) {
            DataObjects.RegistrationKey errorKey = new();
            errorKey.ActionResponse.Messages.Add("Error Generating Registration Keys");
            errorKey.ActionResponse.Messages.AddRange(RecurseException(ex));
            output.Add(errorKey);
        }

        return output;
    }

    public async Task<List<DataObjects.ApiClientToken>> GetApiClientTokens(Guid TenantId)
    {
        List<DataObjects.ApiClientToken> output = new();

        List<EFModels.EFModels.ApiClientToken> recs = await data.ApiClientTokens
            .Where(x => x.TenantId == TenantId)
            .OrderByDescending(x => x.Created)
            .ToListAsync();

        if (recs.Any()) {
            // Load agent names in batch
            List<Guid> agentIds = recs.Select(x => x.AgentId).Distinct().ToList();
            List<EFModels.EFModels.Agent> agents = await data.Agents
                .Where(x => agentIds.Contains(x.AgentId))
                .ToListAsync();

            foreach (EFModels.EFModels.ApiClientToken rec in recs) {
                EFModels.EFModels.Agent? agent = agents.FirstOrDefault(x => x.AgentId == rec.AgentId);

                output.Add(new DataObjects.ApiClientToken {
                    ActionResponse = GetNewActionResponse(true),
                    ApiClientTokenId = rec.ApiClientTokenId,
                    AgentId = rec.AgentId,
                    TenantId = rec.TenantId,
                    TokenHash = rec.TokenHash,
                    TokenPrefix = rec.TokenPrefix ?? string.Empty,
                    Active = rec.Active,
                    Created = rec.Created,
                    RevokedAt = rec.RevokedAt,
                    RevokedBy = rec.RevokedBy ?? string.Empty,
                    AgentName = agent?.Name ?? string.Empty,
                });
            }
        }

        return output;
    }

    public async Task<List<DataObjects.RegistrationKey>> GetRegistrationKeys(Guid TenantId)
    {
        List<DataObjects.RegistrationKey> output = new();

        List<EFModels.EFModels.RegistrationKey> recs = await data.RegistrationKeys
            .Where(x => x.TenantId == TenantId)
            .OrderByDescending(x => x.Created)
            .ToListAsync();

        foreach (EFModels.EFModels.RegistrationKey rec in recs) {
            output.Add(new DataObjects.RegistrationKey {
                ActionResponse = GetNewActionResponse(true),
                RegistrationKeyId = rec.RegistrationKeyId,
                TenantId = rec.TenantId,
                KeyHash = rec.KeyHash,
                KeyPrefix = rec.KeyPrefix ?? string.Empty,
                ExpiresAt = rec.ExpiresAt,
                Used = rec.Used,
                UsedByAgentId = rec.UsedByAgentId,
                UsedAt = rec.UsedAt,
                Created = rec.Created,
                CreatedBy = LastModifiedDisplayName(rec.CreatedBy ?? string.Empty),
            });
        }

        return output;
    }
    private static string HashKey(string plaintext)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToBase64String(hash);
    }

    public async Task<DataObjects.BooleanResponse> RevokeApiClientToken(Guid TokenId, DataObjects.User? CurrentUser = null)
    {
        DataObjects.BooleanResponse output = new();

        EFModels.EFModels.ApiClientToken? rec = await data.ApiClientTokens.FirstOrDefaultAsync(x => x.ApiClientTokenId == TokenId);

        if (rec == null) {
            output.Messages.Add("API Client Token '" + TokenId.ToString() + "' Not Found");
            return output;
        }

        try {
            rec.Active = false;
            rec.RevokedAt = DateTime.UtcNow;
            rec.RevokedBy = CurrentUserIdString(CurrentUser);

            await data.SaveChangesAsync();
            output.Result = true;
        } catch (Exception ex) {
            output.Messages.Add("Error Revoking API Client Token " + TokenId.ToString());
            output.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }

    public async Task<DataObjects.ApiClientToken?> ValidateApiClientToken(string PlaintextToken)
    {
        if (string.IsNullOrWhiteSpace(PlaintextToken)) {
            return null;
        }

        string hash = HashKey(PlaintextToken);

        EFModels.EFModels.ApiClientToken? rec = await data.ApiClientTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash && x.Active == true);

        if (rec == null) {
            return null;
        }

        EFModels.EFModels.Agent? agent = await data.Agents.FirstOrDefaultAsync(x => x.AgentId == rec.AgentId);

        return new DataObjects.ApiClientToken {
            ActionResponse = GetNewActionResponse(true),
            ApiClientTokenId = rec.ApiClientTokenId,
            AgentId = rec.AgentId,
            TenantId = rec.TenantId,
            TokenHash = rec.TokenHash,
            TokenPrefix = rec.TokenPrefix ?? string.Empty,
            Active = rec.Active,
            Created = rec.Created,
            RevokedAt = rec.RevokedAt,
            RevokedBy = rec.RevokedBy ?? string.Empty,
            AgentName = agent?.Name ?? string.Empty,
        };
    }

    public async Task<DataObjects.RegistrationKey?> ValidateRegistrationKey(string PlaintextKey, Guid TenantId)
    {
        if (string.IsNullOrWhiteSpace(PlaintextKey)) {
            return null;
        }

        string hash = HashKey(PlaintextKey);
        DateTime now = DateTime.UtcNow;

        EFModels.EFModels.RegistrationKey? rec = await data.RegistrationKeys
            .FirstOrDefaultAsync(x => x.KeyHash == hash && x.Used == false && x.ExpiresAt > now);

        if (rec == null) {
            return null;
        }

        return new DataObjects.RegistrationKey {
            ActionResponse = GetNewActionResponse(true),
            RegistrationKeyId = rec.RegistrationKeyId,
            TenantId = rec.TenantId,
            KeyHash = rec.KeyHash,
            KeyPrefix = rec.KeyPrefix ?? string.Empty,
            ExpiresAt = rec.ExpiresAt,
            Used = rec.Used,
            UsedByAgentId = rec.UsedByAgentId,
            UsedAt = rec.UsedAt,
            Created = rec.Created,
            CreatedBy = rec.CreatedBy ?? string.Empty,
        };
    }
}

// ---- FreeServicesHub.App.DataAccess.CiCdToken.cs ----
public partial interface IDataAccess
{
    Task<bool> ConsumeCiCdTokenUse(string plaintextToken, Guid tenantId);
    Task<bool> InvalidateCiCdToken(string plaintextToken, Guid tenantId);
}

public partial class DataAccess
{
    /// <summary>
    /// Validates a CiCd token can still be used (has remaining uses, not expired, not invalidated).
    /// On first use, auto-creates a DB tracking record. Each call decrements remaining uses.
    /// When uses are exhausted, the token is automatically invalidated.
    /// Returns true if the use was consumed successfully.
    /// </summary>
    public async Task<bool> ConsumeCiCdTokenUse(string plaintextToken, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(plaintextToken)) return false;

        string hash = HashKey(plaintextToken);
        string prefix = plaintextToken.Length >= 8 ? plaintextToken.Substring(0, 8) : plaintextToken;
        DateTime now = DateTime.UtcNow;

        try {
            // Look up existing tracking record by hash
            var rec = await data.CiCdTokenUsages
                .FirstOrDefaultAsync(x => x.TokenHash == hash && x.TenantId == tenantId);

            if (rec == null) {
                // First use of this token — create tracking record
                int maxUses = ConfigurationHelper?.CiCdMaxAgentRegistrations ?? 10;

                rec = new EFModels.EFModels.CiCdTokenUsage {
                    CiCdTokenUsageId = Guid.NewGuid(),
                    TenantId = tenantId,
                    TokenHash = hash,
                    TokenPrefix = prefix,
                    MaxUses = maxUses,
                    UsesConsumed = 0,
                    Active = true,
                    ExpiresAt = now.AddHours(2), // Token valid for 2 hours (covers one pipeline run)
                    Created = now,
                };

                await data.CiCdTokenUsages.AddAsync(rec);
                await data.SaveChangesAsync();
            }

            // Check if token is still valid
            if (!rec.Active || rec.ExpiresAt <= now || rec.InvalidatedAt != null) {
                return false;
            }

            if (rec.UsesConsumed >= rec.MaxUses) {
                // Already exhausted — auto-invalidate
                rec.Active = false;
                rec.InvalidatedAt = now;
                await data.SaveChangesAsync();
                return false;
            }

            // Consume one use
            rec.UsesConsumed++;

            // Auto-invalidate if all uses consumed
            if (rec.UsesConsumed >= rec.MaxUses) {
                rec.Active = false;
                rec.InvalidatedAt = now;
            }

            await data.SaveChangesAsync();
            return true;
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"ConsumeCiCdTokenUse error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Explicitly invalidates a CiCd token (called at end of pipeline to burn it).
    /// Returns true if the token was found and invalidated.
    /// </summary>
    public async Task<bool> InvalidateCiCdToken(string plaintextToken, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(plaintextToken)) return false;

        string hash = HashKey(plaintextToken);

        try {
            var rec = await data.CiCdTokenUsages
                .FirstOrDefaultAsync(x => x.TokenHash == hash && x.TenantId == tenantId);

            if (rec == null) return false;

            rec.Active = false;
            rec.InvalidatedAt = DateTime.UtcNow;
            await data.SaveChangesAsync();

            return true;
        } catch {
            return false;
        }
    }
}

// ---- FreeServicesHub.App.DataAccess.Heartbeats.cs ----
public partial interface IDataAccess
{
    Task<List<DataObjects.AgentHeartbeat>> GetHeartbeats(Guid AgentId, int Hours = 24);
    Task<DataObjects.BooleanResponse> PruneHeartbeats(int RetentionHours);
    Task<DataObjects.BooleanResponse> SaveHeartbeat(DataObjects.AgentHeartbeat Heartbeat);
}

public partial class DataAccess
{
    public async Task<List<DataObjects.AgentHeartbeat>> GetHeartbeats(Guid AgentId, int Hours = 24)
    {
        List<DataObjects.AgentHeartbeat> output = new();

        DateTime cutoff = DateTime.UtcNow.AddHours(-Hours);

        List<EFModels.EFModels.AgentHeartbeat> recs = await data.AgentHeartbeats
            .Where(x => x.AgentId == AgentId && x.Timestamp >= cutoff)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();

        if (recs.Any()) {
            // Load agent name for display
            EFModels.EFModels.Agent? agent = await data.Agents.FirstOrDefaultAsync(x => x.AgentId == AgentId);

            foreach (EFModels.EFModels.AgentHeartbeat rec in recs) {
                output.Add(new DataObjects.AgentHeartbeat {
                    HeartbeatId = rec.HeartbeatId,
                    AgentId = rec.AgentId,
                    Timestamp = rec.Timestamp,
                    CpuPercent = rec.CpuPercent,
                    MemoryPercent = rec.MemoryPercent,
                    MemoryUsedGB = rec.MemoryUsedGB,
                    MemoryTotalGB = rec.MemoryTotalGB,
                    DiskMetricsJson = rec.DiskMetricsJson ?? string.Empty,
                    CustomDataJson = rec.CustomDataJson ?? string.Empty,
                    AgentName = agent?.Name ?? string.Empty,
                });
            }
        }

        return output;
    }

    public async Task<DataObjects.BooleanResponse> PruneHeartbeats(int RetentionHours)
    {
        DataObjects.BooleanResponse output = new();

        try {
            DateTime cutoff = DateTime.UtcNow.AddHours(-RetentionHours);

            List<EFModels.EFModels.AgentHeartbeat> oldRecords = await data.AgentHeartbeats
                .Where(x => x.Timestamp < cutoff)
                .ToListAsync();

            if (oldRecords.Any()) {
                data.AgentHeartbeats.RemoveRange(oldRecords);
                await data.SaveChangesAsync();
            }

            output.Result = true;
        } catch (Exception ex) {
            output.Messages.Add("Error Pruning Heartbeats");
            output.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }
    public async Task<DataObjects.BooleanResponse> SaveHeartbeat(DataObjects.AgentHeartbeat Heartbeat)
    {
        DataObjects.BooleanResponse output = new();

        DateTime now = DateTime.UtcNow;

        try {
            if (Heartbeat.HeartbeatId == Guid.Empty) {
                Heartbeat.HeartbeatId = Guid.NewGuid();
            }

            if (Heartbeat.Timestamp == default) {
                Heartbeat.Timestamp = now;
            }

            EFModels.EFModels.AgentHeartbeat rec = new() {
                HeartbeatId = Heartbeat.HeartbeatId,
                AgentId = Heartbeat.AgentId,
                Timestamp = Heartbeat.Timestamp,
                CpuPercent = Heartbeat.CpuPercent,
                MemoryPercent = Heartbeat.MemoryPercent,
                MemoryUsedGB = Heartbeat.MemoryUsedGB,
                MemoryTotalGB = Heartbeat.MemoryTotalGB,
                DiskMetricsJson = Heartbeat.DiskMetricsJson,
                CustomDataJson = Heartbeat.CustomDataJson,
            };

            await data.AgentHeartbeats.AddAsync(rec);
            await data.SaveChangesAsync();

            // Update Agent record with latest heartbeat time and status
            EFModels.EFModels.Agent? agent = await data.Agents.FirstOrDefaultAsync(x => x.AgentId == Heartbeat.AgentId);

            if (agent != null) {
                agent.LastHeartbeat = now;

                // Determine status based on thresholds
                int cpuWarning = ConfigurationHelper?.CpuWarningThreshold ?? 70;
                int cpuError = ConfigurationHelper?.CpuErrorThreshold ?? 90;
                int memWarning = ConfigurationHelper?.MemoryWarningThreshold ?? 70;
                int memError = ConfigurationHelper?.MemoryErrorThreshold ?? 90;

                if (Heartbeat.CpuPercent >= cpuError || Heartbeat.MemoryPercent >= memError) {
                    agent.Status = DataObjects.AgentStatuses.Error;
                } else if (Heartbeat.CpuPercent >= cpuWarning || Heartbeat.MemoryPercent >= memWarning) {
                    agent.Status = DataObjects.AgentStatuses.Warning;
                } else {
                    agent.Status = DataObjects.AgentStatuses.Online;
                }

                agent.LastModified = now;

                await data.SaveChangesAsync();

                await SignalRUpdate(new DataObjects.SignalRUpdate {
                    TenantId = agent.TenantId,
                    ItemId = agent.AgentId,
                    UpdateType = DataObjects.SignalRUpdateType.AgentHeartbeat,
                    Message = agent.Status,
                    Object = Heartbeat,
                });
            }

            output.Result = true;
        } catch (Exception ex) {
            output.Messages.Add("Error Saving Heartbeat");
            output.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }
}

// ---- FreeServicesHub.App.DataAccess.Jobs.cs ----
public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> DeleteHubJobs(List<Guid>? Ids, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.HubJob>> GetHubJobs(List<Guid>? Ids, Guid TenantId, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.HubJob>> GetJobsForAgent(Guid AgentId, Guid TenantId);
    Task<List<DataObjects.HubJob>> SaveHubJobs(List<DataObjects.HubJob> Items, DataObjects.User? CurrentUser = null);
}

public partial class DataAccess
{
    public async Task<DataObjects.BooleanResponse> DeleteHubJobs(List<Guid>? Ids, DataObjects.User? CurrentUser = null)
    {
        DataObjects.BooleanResponse output = new();

        if (Ids == null || !Ids.Any()) {
            output.Messages.Add("No Job Ids provided for deletion.");
            return output;
        }

        DateTime now = DateTime.UtcNow;

        try {
            List<EFModels.EFModels.HubJob> recs = await data.HubJobs.Where(x => Ids.Contains(x.HubJobId)).ToListAsync();

            if (!recs.Any()) {
                output.Messages.Add("No matching Job records found.");
                return output;
            }

            foreach (EFModels.EFModels.HubJob rec in recs) {
                rec.Deleted = true;
                rec.DeletedAt = now;
                rec.LastModified = now;

                if (CurrentUser != null) {
                    rec.LastModifiedBy = CurrentUserIdString(CurrentUser);
                }
            }

            await data.SaveChangesAsync();
            output.Result = true;

            foreach (EFModels.EFModels.HubJob rec in recs) {
                await SignalRUpdate(new DataObjects.SignalRUpdate {
                    TenantId = rec.TenantId,
                    ItemId = rec.HubJobId,
                    UpdateType = DataObjects.SignalRUpdateType.JobUpdated,
                    Message = "Deleted",
                    UserId = CurrentUserId(CurrentUser),
                });
            }
        } catch (Exception ex) {
            output.Messages.Add("Error Deleting Jobs");
            output.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }
    public async Task<List<DataObjects.HubJob>> GetHubJobs(List<Guid>? Ids, Guid TenantId, DataObjects.User? CurrentUser = null)
    {
        List<DataObjects.HubJob> output = new();

        List<EFModels.EFModels.HubJob>? recs = null;

        if (Ids != null && Ids.Any()) {
            if (AdminUser(CurrentUser)) {
                recs = await data.HubJobs.Where(x => x.TenantId == TenantId && Ids.Contains(x.HubJobId)).ToListAsync();
            } else {
                recs = await data.HubJobs.Where(x => x.TenantId == TenantId && Ids.Contains(x.HubJobId) && x.Deleted != true).ToListAsync();
            }
        } else {
            if (AdminUser(CurrentUser)) {
                recs = await data.HubJobs.Where(x => x.TenantId == TenantId).ToListAsync();
            } else {
                recs = await data.HubJobs.Where(x => x.TenantId == TenantId && x.Deleted != true).ToListAsync();
            }
        }

        if (recs != null && recs.Any()) {
            foreach (EFModels.EFModels.HubJob rec in recs) {
                output.Add(new DataObjects.HubJob {
                    ActionResponse = GetNewActionResponse(true),
                    HubJobId = rec.HubJobId,
                    TenantId = rec.TenantId,
                    AgentId = rec.AgentId,
                    JobType = rec.JobType,
                    Status = rec.Status ?? string.Empty,
                    Payload = rec.Payload ?? string.Empty,
                    Result = rec.Result ?? string.Empty,
                    ErrorMessage = rec.ErrorMessage ?? string.Empty,
                    Priority = rec.Priority,
                    MaxRetries = rec.MaxRetries,
                    RetryCount = rec.RetryCount,
                    Created = rec.Created,
                    CreatedBy = LastModifiedDisplayName(rec.CreatedBy),
                    StartedAt = rec.StartedAt,
                    CompletedAt = rec.CompletedAt,
                    LastModified = rec.LastModified,
                    LastModifiedBy = LastModifiedDisplayName(rec.LastModifiedBy),
                    Deleted = rec.Deleted,
                    DeletedAt = rec.DeletedAt,
                });
            }
        }

        return output;
    }

    public async Task<List<DataObjects.HubJob>> GetJobsForAgent(Guid AgentId, Guid TenantId)
    {
        List<DataObjects.HubJob> output = new();

        var recs = await data.HubJobs
            .Where(x => x.TenantId == TenantId && !x.Deleted
                && (x.AgentId == AgentId || (x.Status == "Queued" && x.AgentId == null)))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Created)
            .ToListAsync();

        foreach (EFModels.EFModels.HubJob rec in recs) {
            output.Add(new DataObjects.HubJob {
                ActionResponse = GetNewActionResponse(true),
                HubJobId = rec.HubJobId,
                TenantId = rec.TenantId,
                AgentId = rec.AgentId,
                JobType = rec.JobType,
                Status = rec.Status ?? string.Empty,
                Payload = rec.Payload ?? string.Empty,
                Result = rec.Result ?? string.Empty,
                ErrorMessage = rec.ErrorMessage ?? string.Empty,
                Priority = rec.Priority,
                MaxRetries = rec.MaxRetries,
                RetryCount = rec.RetryCount,
                Created = rec.Created,
                CreatedBy = LastModifiedDisplayName(rec.CreatedBy),
                StartedAt = rec.StartedAt,
                CompletedAt = rec.CompletedAt,
                LastModified = rec.LastModified,
                LastModifiedBy = LastModifiedDisplayName(rec.LastModifiedBy),
                Deleted = rec.Deleted,
                DeletedAt = rec.DeletedAt,
            });
        }

        return output;
    }

    private async Task<DataObjects.HubJob> SaveHubJob(DataObjects.HubJob job, DataObjects.User? CurrentUser = null)
    {
        DataObjects.HubJob output = job;
        output.ActionResponse = GetNewActionResponse();

        bool newRecord = false;
        DateTime now = DateTime.UtcNow;

        EFModels.EFModels.HubJob? rec = await data.HubJobs.FirstOrDefaultAsync(x => x.HubJobId == output.HubJobId);

        if (rec != null && rec.Deleted) {
            if (AdminUser(CurrentUser)) {
                // Ok to edit this record that is marked as deleted.
            } else {
                output.ActionResponse.Messages.Add("Job '" + output.HubJobId.ToString() + "' No Longer Exists");
                return output;
            }
        }

        string? previousStatus = rec?.Status;

        if (rec == null) {
            if (output.HubJobId == Guid.Empty) {
                newRecord = true;
                output.HubJobId = Guid.NewGuid();

                rec = new EFModels.EFModels.HubJob {
                    HubJobId = output.HubJobId,
                    TenantId = output.TenantId,
                    Deleted = false,
                    Created = now,
                    CreatedBy = CurrentUserIdString(CurrentUser),
                };
            } else {
                output.ActionResponse.Messages.Add("Job '" + output.HubJobId.ToString() + "' No Longer Exists");
                return output;
            }
        }

        output.JobType = MaxStringLength(output.JobType, 100);
        output.Status = MaxStringLength(output.Status, 50);

        rec.AgentId = output.AgentId;
        rec.JobType = output.JobType;
        rec.Status = output.Status;
        rec.Payload = output.Payload;
        rec.Result = output.Result;
        rec.ErrorMessage = output.ErrorMessage;
        rec.Priority = output.Priority;
        rec.MaxRetries = output.MaxRetries;
        rec.RetryCount = output.RetryCount;
        rec.StartedAt = output.StartedAt;
        rec.CompletedAt = output.CompletedAt;
        rec.LastModified = now;
        rec.LastModifiedBy = CurrentUserIdString(CurrentUser);

        if (AdminUser(CurrentUser)) {
            rec.Deleted = output.Deleted;

            if (!output.Deleted) {
                rec.DeletedAt = null;
            }
        }

        try {
            if (newRecord) {
                await data.HubJobs.AddAsync(rec);
            }
            await data.SaveChangesAsync();

            output.ActionResponse.Result = true;

            string updateType = DataObjects.SignalRUpdateType.JobUpdated;
            if (output.Status == DataObjects.HubJobStatuses.Completed
                || output.Status == DataObjects.HubJobStatuses.Failed
                || output.Status == DataObjects.HubJobStatuses.Cancelled) {
                updateType = DataObjects.SignalRUpdateType.JobCompleted;
            }

            await SignalRUpdate(new DataObjects.SignalRUpdate {
                TenantId = output.TenantId,
                ItemId = output.HubJobId,
                UpdateType = updateType,
                Message = "Saved",
                UserId = CurrentUserId(CurrentUser),
                Object = output,
            });
        } catch (Exception ex) {
            output.ActionResponse.Messages.Add("Error Saving Job " + output.HubJobId.ToString());
            output.ActionResponse.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.HubJob>> SaveHubJobs(List<DataObjects.HubJob> Items, DataObjects.User? CurrentUser = null)
    {
        List<DataObjects.HubJob> output = new();

        foreach (DataObjects.HubJob item in Items) {
            DataObjects.HubJob saved = await SaveHubJob(item, CurrentUser);
            output.Add(saved);
        }

        return output;
    }
}

// ---- FreeServicesHub.App.DataAccess.Registration.cs ----
public partial interface IDataAccess
{
    Task<DataObjects.AgentRegistrationResponse> RegisterAgent(DataObjects.AgentRegistrationRequest Request, Guid TenantId);
}

public partial class DataAccess
{
    public async Task<DataObjects.AgentRegistrationResponse> RegisterAgent(DataObjects.AgentRegistrationRequest Request, Guid TenantId)
    {
        DataObjects.AgentRegistrationResponse output = new();

        // Validate the registration key (hash-only lookup, TenantId comes from the key)
        DataObjects.RegistrationKey? regKey = await ValidateRegistrationKey(Request.RegistrationKey, TenantId);

        if (regKey == null) {
            output.ActionResponse.Messages.Add("Invalid, expired, or already-used registration key.");
            return output;
        }

        // Use the key's TenantId (set during CiCd key generation) so the agent
        // lands in the correct tenant even though this endpoint is anonymous.
        Guid agentTenantId = regKey.TenantId;

        DateTime now = DateTime.UtcNow;

        try {
            // Upsert: if an agent with the same name already exists, update it
            EFModels.EFModels.Agent? agentRec = await data.Agents
                .FirstOrDefaultAsync(x => x.Name == Request.Hostname);

            Guid agentId;
            bool isNewAgent;

            if (agentRec != null) {
                // Existing agent — update in place
                agentId = agentRec.AgentId;
                isNewAgent = false;

                agentRec.TenantId = agentTenantId;
                agentRec.Hostname = Request.Hostname;
                agentRec.OperatingSystem = Request.OperatingSystem;
                agentRec.Architecture = Request.Architecture;
                agentRec.AgentVersion = Request.AgentVersion;
                agentRec.DotNetVersion = Request.DotNetVersion;
                agentRec.Status = DataObjects.AgentStatuses.Online;
                agentRec.LastHeartbeat = now;
                agentRec.RegisteredAt = now;
                agentRec.RegisteredBy = "RegistrationKey:" + regKey.KeyPrefix;
                agentRec.LastModified = now;
                agentRec.LastModifiedBy = "RegistrationKey:" + regKey.KeyPrefix;
                agentRec.Deleted = false;
                agentRec.DeletedAt = null;

                // Revoke all existing API tokens for this agent
                var oldTokens = await data.ApiClientTokens
                    .Where(x => x.AgentId == agentId && x.Active == true)
                    .ToListAsync();
                foreach (var t in oldTokens) {
                    t.Active = false;
                    t.RevokedAt = now;
                    t.RevokedBy = "Re-registration";
                }
            } else {
                // New agent
                agentId = Guid.NewGuid();
                isNewAgent = true;

                agentRec = new() {
                    AgentId = agentId,
                    TenantId = agentTenantId,
                    Name = Request.Hostname,
                    Hostname = Request.Hostname,
                    OperatingSystem = Request.OperatingSystem,
                    Architecture = Request.Architecture,
                    AgentVersion = Request.AgentVersion,
                    DotNetVersion = Request.DotNetVersion,
                    Status = DataObjects.AgentStatuses.Online,
                    LastHeartbeat = now,
                    RegisteredAt = now,
                    RegisteredBy = "RegistrationKey:" + regKey.KeyPrefix,
                    Added = now,
                    AddedBy = "RegistrationKey:" + regKey.KeyPrefix,
                    LastModified = now,
                    LastModifiedBy = "RegistrationKey:" + regKey.KeyPrefix,
                    Deleted = false,
                };

                await data.Agents.AddAsync(agentRec);
            }

            await data.SaveChangesAsync();

            // Burn the registration key
            EFModels.EFModels.RegistrationKey? regKeyRec = await data.RegistrationKeys
                .FirstOrDefaultAsync(x => x.RegistrationKeyId == regKey.RegistrationKeyId);

            if (regKeyRec != null) {
                regKeyRec.Used = true;
                regKeyRec.UsedByAgentId = agentId;
                regKeyRec.UsedAt = now;
                await data.SaveChangesAsync();
            }

            // Generate a fresh API client token for the agent
            DataObjects.ApiClientToken token = await GenerateApiClientToken(agentId, agentTenantId);

            if (!token.ActionResponse.Result) {
                output.ActionResponse.Messages.Add("Agent created but failed to generate API client token.");
                output.ActionResponse.Messages.AddRange(token.ActionResponse.Messages);
                return output;
            }

            output.ActionResponse = GetNewActionResponse(true);
            output.AgentId = agentId;
            output.ApiClientToken = token.NewTokenPlaintext;

            await SignalRUpdate(new DataObjects.SignalRUpdate {
                TenantId = agentTenantId,
                ItemId = agentId,
                UpdateType = DataObjects.SignalRUpdateType.AgentConnected,
                Message = (isNewAgent ? "Agent Registered: " : "Agent Re-registered: ") + Request.Hostname,
            });
        } catch (Exception ex) {
            output.ActionResponse.Messages.Add("Error Registering Agent");
            output.ActionResponse.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }
}
