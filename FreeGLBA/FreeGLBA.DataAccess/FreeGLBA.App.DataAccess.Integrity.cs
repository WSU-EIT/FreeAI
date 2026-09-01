using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FreeGLBA;

// ============================================================================
// TAMPER-EVIDENT EVENT HASH CHAIN
// Every ingested event is stamped with a per-source-system chain position
// (ChainSequence), the previous event's hash (PrevRowHash), and a SHA-256 over
// its own immutable audit fields (RowHash). Verification then detects three
// kinds of tampering: modified rows (content mismatch), broken links, and
// deleted rows (sequence gaps). Legitimate UI edits of an audit record break
// its hash BY DESIGN - an audit trail is supposed to be immutable, and
// verification surfaces exactly which records changed after the fact.
//
// Sequence assignment is serialized with an in-process lock; when multiple app
// instances ingest concurrently, chains can fork (documented limitation).
// ============================================================================

public partial interface IDataAccess
{
    /// <summary>
    /// Verifies a source system's tamper-evident hash chain: recomputes every
    /// chained event's hash, checks the links between events, and checks the
    /// sequence for gaps left by deletions.
    /// </summary>
    Task<DataObjects.ChainVerificationResult> VerifyAccessEventChainAsync(Guid sourceSystemId);
}

public partial class DataAccess
{
    #region Event Hash Chain

    /// <summary>Serializes chain-position assignment within this process.</summary>
    private static readonly SemaphoreSlim _chainLock = new(1, 1);

    /// <summary>
    /// Canonical representation of the fields protected by the hash. Anything
    /// listed here becomes tamper-evident; timestamps use ticks so formatting
    /// can never shift the hash.
    /// </summary>
    private static string CanonicalEventString(EFModels.EFModels.AccessEventItem evt)
    {
        return string.Join('|',
            evt.AccessEventId.ToString("N"),
            evt.SourceSystemId.ToString("N"),
            evt.SourceEventId,
            evt.AccessedAt.Ticks,
            evt.ReceivedAt.Ticks,
            evt.UserId,
            evt.UserName,
            evt.SubjectId,
            evt.SubjectIds,
            evt.SubjectCount,
            evt.SubjectType,
            evt.AccessType,
            evt.DataCategory,
            evt.Purpose,
            evt.DataOwnerName,
            evt.PrevRowHash,
            evt.ChainSequence);
    }

    private static string ComputeEventRowHash(EFModels.EFModels.AccessEventItem evt)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalEventString(evt)));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Stamps ChainSequence, PrevRowHash, and RowHash onto a new event. The
    /// caller must hold <see cref="_chainLock"/> and save before releasing it.
    /// Pass the previous event of the same batch via <paramref name="pendingTail"/>
    /// when several events are chained before a single save.
    /// </summary>
    private async Task AssignChainPositionAsync(EFModels.EFModels.AccessEventItem evt,
        EFModels.EFModels.AccessEventItem? pendingTail = null)
    {
        if (pendingTail != null && pendingTail.SourceSystemId == evt.SourceSystemId) {
            evt.ChainSequence = pendingTail.ChainSequence + 1;
            evt.PrevRowHash = pendingTail.RowHash;
        } else {
            var tail = await data.AccessEvents
                .AsNoTracking()
                .Where(x => x.SourceSystemId == evt.SourceSystemId && x.ChainSequence > 0)
                .OrderByDescending(x => x.ChainSequence)
                .FirstOrDefaultAsync();
            evt.ChainSequence = (tail?.ChainSequence ?? 0) + 1;
            evt.PrevRowHash = tail?.RowHash ?? string.Empty;
        }

        evt.RowHash = ComputeEventRowHash(evt);
    }

    public async Task<DataObjects.ChainVerificationResult> VerifyAccessEventChainAsync(Guid sourceSystemId)
    {
        var output = new DataObjects.ChainVerificationResult {
            SourceSystemId = sourceSystemId,
            VerifiedAt = DateTime.UtcNow,
        };

        var source = await data.SourceSystems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SourceSystemId == sourceSystemId);
        output.SourceSystemName = source?.Name ?? string.Empty;

        output.UnhashedEvents = await data.AccessEvents
            .CountAsync(x => x.SourceSystemId == sourceSystemId && x.ChainSequence == 0);

        var events = await data.AccessEvents
            .AsNoTracking()
            .Where(x => x.SourceSystemId == sourceSystemId && x.ChainSequence > 0)
            .OrderBy(x => x.ChainSequence)
            .ToListAsync();
        output.EventsChecked = events.Count;

        EFModels.EFModels.AccessEventItem? previous = null;
        foreach (var evt in events) {
            // A modified row no longer matches the hash computed at ingest.
            if (ComputeEventRowHash(evt) != evt.RowHash) {
                output.Issues.Add(new DataObjects.ChainVerificationIssue {
                    AccessEventId = evt.AccessEventId,
                    ChainSequence = evt.ChainSequence,
                    IssueType = "ContentMismatch",
                    Detail = $"Event #{evt.ChainSequence} ({evt.AccessedAt:yyyy-MM-dd HH:mm} UTC, user {evt.UserId}) no longer matches the hash recorded at ingest - its content was modified after the fact.",
                });
            }

            if (previous != null) {
                // A deleted row leaves a hole in the sequence.
                if (evt.ChainSequence != previous.ChainSequence + 1) {
                    output.Issues.Add(new DataObjects.ChainVerificationIssue {
                        AccessEventId = evt.AccessEventId,
                        ChainSequence = evt.ChainSequence,
                        IssueType = "SequenceGap",
                        Detail = $"Sequence jumps from #{previous.ChainSequence} to #{evt.ChainSequence} - {evt.ChainSequence - previous.ChainSequence - 1} event(s) were deleted from the chain.",
                    });
                } else if (evt.PrevRowHash != previous.RowHash) {
                    output.Issues.Add(new DataObjects.ChainVerificationIssue {
                        AccessEventId = evt.AccessEventId,
                        ChainSequence = evt.ChainSequence,
                        IssueType = "BrokenLink",
                        Detail = $"Event #{evt.ChainSequence} does not link to the recorded hash of event #{previous.ChainSequence}.",
                    });
                }
            } else if (evt.ChainSequence != 1) {
                output.Issues.Add(new DataObjects.ChainVerificationIssue {
                    AccessEventId = evt.AccessEventId,
                    ChainSequence = evt.ChainSequence,
                    IssueType = "SequenceGap",
                    Detail = $"Chain starts at #{evt.ChainSequence} - the first {evt.ChainSequence - 1} event(s) were deleted.",
                });
            }

            previous = evt;
        }

        return output;
    }

    #endregion
}
