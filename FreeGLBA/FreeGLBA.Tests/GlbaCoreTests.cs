using FreeGLBA;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FreeGLBA.Tests;

// ============================================================================
// FREEGLBA CORE LOGIC TESTS
// Runs against a real DataAccess over the EF InMemory provider - no server,
// no API keys, CI-friendly: `dotnet test FreeGLBA.Tests`.
//
// The InMemory store name is fixed ("InMemory"), so every DataAccess in the
// process shares one store. All tests therefore live in one collection, use
// unique IDs, and never assume tables are empty.
// ============================================================================

public class DataAccessFixture : IDisposable
{
    public DataAccess DA { get; }

    public DataAccessFixture()
    {
        // PDF generation needs the license set, which Program.cs normally does.
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        DA = new DataAccess(DatabaseType: "InMemory");
    }

    public void Dispose() => DA.Dispose();
}

[CollectionDefinition("DataAccess")]
public class DataAccessCollection : ICollectionFixture<DataAccessFixture> { }

[Collection("DataAccess")]
public class GlbaCoreTests
{
    private readonly DataAccess _da;

    public GlbaCoreTests(DataAccessFixture fixture)
    {
        _da = fixture.DA;
    }

    private async Task<DataObjects.SourceSystem> CreateSourceSystemAsync(
        string? ownerName = null, string? ownerEmail = null, string? ownerDepartment = null, string? savedBy = null)
    {
        var dto = new DataObjects.SourceSystem {
            Name = "Test-" + Guid.NewGuid().ToString("N")[..8],
            DisplayName = "Test System",
            ContactEmail = "tech@test.edu",
            IsActive = true,
            DataOwnerName = ownerName ?? string.Empty,
            DataOwnerEmail = ownerEmail ?? string.Empty,
            DataOwnerDepartment = ownerDepartment ?? string.Empty,
        };
        var saved = await _da.SaveSourceSystemAsync(dto, savedBy);
        Assert.NotNull(saved);
        return saved!;
    }

    // ------------------------------------------------------------ ownership

    [Fact]
    public async Task SaveSourceSystem_WithOwner_SetsLiveFieldsAndOpensHistory()
    {
        var system = await CreateSourceSystemAsync("Alice Nguyen", "alice@test.edu", "Financial Aid", savedBy: "Unit Test");

        Assert.Equal("Alice Nguyen", system.DataOwnerName);
        Assert.NotNull(system.DataOwnerAssignedAt);

        var history = await _da.GetDataOwnershipHistoryAsync(system.SourceSystemId);
        var current = Assert.Single(history);
        Assert.True(current.IsCurrent);
        Assert.Equal("Alice Nguyen", current.OwnerName);
        Assert.Equal("Unit Test", current.AssignedBy);
    }

    [Fact]
    public async Task ChangeOwner_ClosesOldHistoryRowAndOpensNew()
    {
        var system = await CreateSourceSystemAsync("Alice Nguyen", "alice@test.edu", "Financial Aid");

        system.DataOwnerName = "Marcus Chen";
        system.DataOwnerEmail = "marcus@test.edu";
        system.DataOwnerDepartment = "Bursar";
        var updated = await _da.SaveSourceSystemAsync(system, "Unit Test");
        Assert.NotNull(updated);

        var history = await _da.GetDataOwnershipHistoryAsync(system.SourceSystemId);
        Assert.Equal(2, history.Count);

        var current = history.Single(x => x.IsCurrent);
        Assert.Equal("Marcus Chen", current.OwnerName);

        var previous = history.Single(x => !x.IsCurrent);
        Assert.Equal("Alice Nguyen", previous.OwnerName);
        Assert.NotNull(previous.EndedAt);
    }

    [Fact]
    public async Task SaveSourceSystem_UnchangedOwner_AddsNoHistoryRow()
    {
        var system = await CreateSourceSystemAsync("Alice Nguyen", "alice@test.edu", "Financial Aid");

        // Save again with an identical owner (different casing must not count as a change).
        system.DataOwnerName = "alice nguyen";
        await _da.SaveSourceSystemAsync(system);

        var history = await _da.GetDataOwnershipHistoryAsync(system.SourceSystemId);
        Assert.Single(history);
    }

    [Fact]
    public async Task ProcessGlbaEvent_SnapshotsSourceSystemOwner()
    {
        var system = await CreateSourceSystemAsync("Alice Nguyen", "alice@test.edu", "Financial Aid");

        var response = await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
            UserId = "U1",
            SubjectId = "SNAP-" + Guid.NewGuid().ToString("N")[..8],
            AccessType = "View",
            AccessedAt = DateTime.UtcNow,
        }, system.SourceSystemId);

        Assert.Equal("accepted", response.Status);
        Assert.NotNull(response.EventId);

        var stored = await _da.GetAccessEventAsync(response.EventId!.Value);
        Assert.NotNull(stored);
        Assert.Equal("Alice Nguyen", stored!.DataOwnerName);
        Assert.Equal("Financial Aid", stored.DataOwnerDepartment);
        // Live owner is served alongside the snapshot.
        Assert.Equal("Alice Nguyen", stored.CurrentDataOwnerName);
    }

    [Fact]
    public async Task ProcessGlbaEvent_CallerSuppliedOwnerWinsOverSystemOwner()
    {
        var system = await CreateSourceSystemAsync("Alice Nguyen", "alice@test.edu", "Financial Aid");

        var response = await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
            UserId = "U1",
            SubjectId = "OWN-" + Guid.NewGuid().ToString("N")[..8],
            AccessType = "View",
            AccessedAt = DateTime.UtcNow,
            DataOwnerName = "Registrar Records Desk",
            DataOwnerEmail = "records@test.edu",
        }, system.SourceSystemId);

        Assert.Equal("accepted", response.Status);
        var stored = await _da.GetAccessEventAsync(response.EventId!.Value);
        Assert.Equal("Registrar Records Desk", stored!.DataOwnerName);
    }

    [Fact]
    public async Task EventSnapshot_SurvivesOwnershipChange()
    {
        var system = await CreateSourceSystemAsync("Alice Nguyen", "alice@test.edu", "Financial Aid");

        var response = await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
            UserId = "U1",
            SubjectId = "SRV-" + Guid.NewGuid().ToString("N")[..8],
            AccessType = "View",
            AccessedAt = DateTime.UtcNow,
        }, system.SourceSystemId);

        system.DataOwnerName = "Marcus Chen";
        system.DataOwnerEmail = "marcus@test.edu";
        system.DataOwnerDepartment = "Bursar";
        await _da.SaveSourceSystemAsync(system);

        var stored = await _da.GetAccessEventAsync(response.EventId!.Value);
        Assert.Equal("Alice Nguyen", stored!.DataOwnerName);          // then
        Assert.Equal("Marcus Chen", stored.CurrentDataOwnerName);     // now
    }

    // ---------------------------------------------------------- dedupe

    [Fact]
    public async Task ProcessGlbaEvent_SameSourceEventId_ReportsDuplicate()
    {
        var system = await CreateSourceSystemAsync();
        var request = new DataObjects.GlbaEventRequest {
            UserId = "U1",
            SubjectId = "DUP-" + Guid.NewGuid().ToString("N")[..8],
            AccessType = "View",
            SourceEventId = "EVT-" + Guid.NewGuid().ToString("N"),
            AccessedAt = DateTime.UtcNow,
        };

        var first = await _da.ProcessGlbaEventAsync(request, system.SourceSystemId);
        var second = await _da.ProcessGlbaEventAsync(request, system.SourceSystemId);

        Assert.Equal("accepted", first.Status);
        Assert.Equal("duplicate", second.Status);
    }

    [Fact]
    public async Task ProcessGlbaEvent_MissingRequiredFields_IsRejected()
    {
        var system = await CreateSourceSystemAsync();

        var noUser = await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
            SubjectId = "S1", AccessType = "View",
        }, system.SourceSystemId);
        Assert.Equal("error", noUser.Status);

        var noType = await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
            UserId = "U1", SubjectId = "S1",
        }, system.SourceSystemId);
        Assert.Equal("error", noType.Status);
    }

    // ------------------------------------------------------ subject stats

    [Fact]
    public async Task SubjectStats_ExactCountsAndEventTimes_OnInsertAndDelete()
    {
        var system = await CreateSourceSystemAsync();
        var subjectId = "STAT-" + Guid.NewGuid().ToString("N")[..8];
        var earlier = DateTime.UtcNow.AddDays(-10);
        var later = DateTime.UtcNow.AddDays(-1);

        var firstEvent = await _da.SaveAccessEventAsync(new DataObjects.AccessEvent {
            SourceSystemId = system.SourceSystemId,
            UserId = "USER-A", SubjectId = subjectId, AccessType = "View", AccessedAt = earlier,
        });
        await _da.SaveAccessEventAsync(new DataObjects.AccessEvent {
            SourceSystemId = system.SourceSystemId,
            UserId = "USER-B", SubjectId = subjectId, AccessType = "Export", AccessedAt = later,
        });

        var subjects = await _da.GetDataSubjectsAsync(new DataObjects.DataSubjectFilter { Search = subjectId });
        var subject = Assert.Single(subjects.Records);
        Assert.Equal(2, subject.TotalAccessCount);
        Assert.Equal(2, subject.UniqueAccessorCount);
        Assert.Equal(earlier, subject.FirstAccessedAt, TimeSpan.FromSeconds(1));
        Assert.Equal(later, subject.LastAccessedAt, TimeSpan.FromSeconds(1));

        // Deleting an event must recompute, not increment.
        Assert.True(await _da.DeleteAccessEventAsync(firstEvent!.AccessEventId));
        subjects = await _da.GetDataSubjectsAsync(new DataObjects.DataSubjectFilter { Search = subjectId });
        subject = Assert.Single(subjects.Records);
        Assert.Equal(1, subject.TotalAccessCount);
        Assert.Equal(1, subject.UniqueAccessorCount);
    }

    [Fact]
    public async Task BulkInsert_SubjectFirstAccess_UsesEventTimeNotIngestTime()
    {
        var system = await CreateSourceSystemAsync();
        var subjectId = "BULK-" + Guid.NewGuid().ToString("N")[..8];
        var backdated = DateTime.UtcNow.AddDays(-20);

        var result = await _da.SaveAccessEventsAsync(new List<DataObjects.AccessEvent> {
            new() { SourceSystemId = system.SourceSystemId, UserId = "U1", SubjectId = subjectId, AccessType = "View", AccessedAt = backdated },
            new() { SourceSystemId = system.SourceSystemId, UserId = "U2", SubjectId = subjectId, AccessType = "View", AccessedAt = DateTime.UtcNow.AddDays(-2) },
        });

        Assert.True(result.Success);
        Assert.Equal(2, result.Saved);

        var subjects = await _da.GetDataSubjectsAsync(new DataObjects.DataSubjectFilter { Search = subjectId });
        var subject = Assert.Single(subjects.Records);
        Assert.Equal(backdated, subject.FirstAccessedAt, TimeSpan.FromSeconds(1));
        Assert.Equal(2, subject.UniqueAccessorCount);
    }

    // ------------------------------------------------- source system delete

    [Fact]
    public async Task DeleteSourceSystem_WithEvents_IsRefused()
    {
        var system = await CreateSourceSystemAsync();
        await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
            UserId = "U1", SubjectId = "DEL-" + Guid.NewGuid().ToString("N")[..8], AccessType = "View",
        }, system.SourceSystemId);

        Assert.False(await _da.DeleteSourceSystemAsync(system.SourceSystemId));
        Assert.NotNull(await _da.GetSourceSystemAsync(system.SourceSystemId));
    }

    [Fact]
    public async Task DeleteSourceSystem_WithoutEvents_RemovesSystemAndHistory()
    {
        var system = await CreateSourceSystemAsync("Owner", "owner@test.edu", "Dept");

        Assert.True(await _da.DeleteSourceSystemAsync(system.SourceSystemId));
        Assert.Null(await _da.GetSourceSystemAsync(system.SourceSystemId));
        Assert.Empty(await _da.GetDataOwnershipHistoryAsync(system.SourceSystemId));
    }

    // -------------------------------------------------------------- reports

    [Fact]
    public async Task GenerateComplianceReportPdf_ProducesValidPdfAndUpdatesRecord()
    {
        var system = await CreateSourceSystemAsync("Alice Nguyen", "alice@test.edu", "Financial Aid");
        await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
            UserId = "U1", SubjectId = "PDF-" + Guid.NewGuid().ToString("N")[..8], AccessType = "View",
            AccessedAt = DateTime.UtcNow,
        }, system.SourceSystemId);

        var report = await _da.SaveComplianceReportAsync(new DataObjects.ComplianceReport {
            ReportType = "Unit Test Report",
            PeriodStart = DateTime.UtcNow.Date.AddDays(-7),
            PeriodEnd = DateTime.UtcNow.Date,
        });
        Assert.NotNull(report);

        var export = await _da.GenerateComplianceReportPdfAsync(report!.ComplianceReportId, "Unit Test");
        Assert.NotNull(export);
        Assert.Equal("application/pdf", export!.ContentType);
        Assert.True(export.Bytes.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(export.Bytes[..4]));

        var refreshed = await _da.GetComplianceReportAsync(report.ComplianceReportId);
        Assert.True(refreshed!.TotalEvents > 0);
        Assert.False(string.IsNullOrWhiteSpace(refreshed.ReportData));
        Assert.Equal("Unit Test", refreshed.GeneratedBy);
    }

    [Fact]
    public async Task GenerateComplianceReportCsv_RowCountMatchesRecordedTotal()
    {
        var system = await CreateSourceSystemAsync();
        await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
            UserId = "U1", SubjectId = "CSV-" + Guid.NewGuid().ToString("N")[..8], AccessType = "View",
            AccessedAt = DateTime.UtcNow,
        }, system.SourceSystemId);

        var report = await _da.SaveComplianceReportAsync(new DataObjects.ComplianceReport {
            ReportType = "Unit Test CSV",
            PeriodStart = DateTime.UtcNow.Date.AddDays(-7),
            PeriodEnd = DateTime.UtcNow.Date,
        });

        var export = await _da.GenerateComplianceReportCsvAsync(report!.ComplianceReportId);
        Assert.NotNull(export);

        var lines = System.Text.Encoding.UTF8.GetString(export!.Bytes)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var refreshed = await _da.GetComplianceReportAsync(report.ComplianceReportId);

        // Header row plus one row per event recorded for the period.
        Assert.Equal(refreshed!.TotalEvents + 1, lines.Length);
    }

    [Fact]
    public async Task GenerateSubjectAccessHistoryPdf_ProducesValidPdf_AndUnknownSubjectReturnsNull()
    {
        var system = await CreateSourceSystemAsync("Alice Nguyen", "alice@test.edu", "Financial Aid");
        var subjectId = "DSAR-" + Guid.NewGuid().ToString("N")[..8];
        await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
            UserId = "U1", SubjectId = subjectId, AccessType = "View", AccessedAt = DateTime.UtcNow,
            Purpose = "Unit test access",
        }, system.SourceSystemId);

        var export = await _da.GenerateSubjectAccessHistoryPdfAsync(subjectId, "Unit Test");
        Assert.NotNull(export);
        Assert.Equal("application/pdf", export!.ContentType);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(export.Bytes[..4]));
        Assert.Contains(subjectId.ToLower(), export.FileName.ToLower());

        Assert.Null(await _da.GenerateSubjectAccessHistoryPdfAsync("NO-SUCH-SUBJECT-" + Guid.NewGuid().ToString("N")));
    }

    // ------------------------------------------------------ integrity chain

    private static EFModels.EFModels.EFDataModel OpenRawContext()
    {
        // The InMemory store is keyed by name process-wide, so a second context
        // reaches the same data DataAccess uses - letting tests tamper with rows
        // the way an attacker with database access would.
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<EFModels.EFModels.EFDataModel>()
            .UseInMemoryDatabase("InMemory")
            .Options;
        return new EFModels.EFModels.EFDataModel(options);
    }

    [Fact]
    public async Task HashChain_IsValidAfterMixedInserts()
    {
        var system = await CreateSourceSystemAsync();
        await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
            UserId = "U1", SubjectId = "CH-" + Guid.NewGuid().ToString("N")[..8], AccessType = "View",
        }, system.SourceSystemId);
        await _da.SaveAccessEventsAsync(new List<DataObjects.AccessEvent> {
            new() { SourceSystemId = system.SourceSystemId, UserId = "U2", SubjectId = "CH-A", AccessType = "View", AccessedAt = DateTime.UtcNow },
            new() { SourceSystemId = system.SourceSystemId, UserId = "U3", SubjectId = "CH-B", AccessType = "Export", AccessedAt = DateTime.UtcNow },
        });
        await _da.SaveAccessEventAsync(new DataObjects.AccessEvent {
            SourceSystemId = system.SourceSystemId, UserId = "U4", SubjectId = "CH-C", AccessType = "View", AccessedAt = DateTime.UtcNow,
        });

        var result = await _da.VerifyAccessEventChainAsync(system.SourceSystemId);
        Assert.True(result.Valid, string.Join("; ", result.Issues.Select(x => x.Detail)));
        Assert.Equal(4, result.EventsChecked);
        Assert.Equal(0, result.UnhashedEvents);
    }

    [Fact]
    public async Task HashChain_DetectsTamperedRow()
    {
        var system = await CreateSourceSystemAsync();
        var response = await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
            UserId = "U1", SubjectId = "TAMPER-" + Guid.NewGuid().ToString("N")[..8], AccessType = "View",
            Purpose = "Original purpose",
        }, system.SourceSystemId);

        // Simulate an attacker editing the stored row directly in the database.
        using (var raw = OpenRawContext()) {
            var row = raw.AccessEvents.First(x => x.AccessEventId == response.EventId!.Value);
            row.Purpose = "Rewritten after the fact";
            raw.SaveChanges();
        }

        var result = await _da.VerifyAccessEventChainAsync(system.SourceSystemId);
        Assert.False(result.Valid);
        Assert.Contains(result.Issues, x => x.IssueType == "ContentMismatch" && x.AccessEventId == response.EventId!.Value);
    }

    [Fact]
    public async Task HashChain_DetectsDeletedRowAsSequenceGap()
    {
        var system = await CreateSourceSystemAsync();
        DataObjects.GlbaEventResponse? middle = null;
        for (var i = 0; i < 3; i++) {
            var response = await _da.ProcessGlbaEventAsync(new DataObjects.GlbaEventRequest {
                UserId = "U1", SubjectId = $"GAP-{i}-" + Guid.NewGuid().ToString("N")[..6], AccessType = "View",
            }, system.SourceSystemId);
            if (i == 1) middle = response;
        }

        Assert.True(await _da.DeleteAccessEventAsync(middle!.EventId!.Value));

        var result = await _da.VerifyAccessEventChainAsync(system.SourceSystemId);
        Assert.False(result.Valid);
        Assert.Contains(result.Issues, x => x.IssueType == "SequenceGap");
    }

    // ------------------------------------------------------ settings + alerts

    [Fact]
    public async Task GlbaSettings_RoundTripAndSanitization()
    {
        var settings = await _da.GetGlbaSettingsAsync();
        settings.AlertsEnabled = true;
        settings.WebhookUrl = "  https://example.test/hook  ";
        settings.BulkExportAlertThreshold = 0;   // sanitized to minimum 2
        settings.BusinessHoursStart = 8;
        settings.BusinessHoursEnd = 5;           // invalid: sanitized past start
        settings.InstitutionTimeZone = "Pacific Standard Time";

        var saved = await _da.SaveGlbaSettingsAsync(settings);
        Assert.Equal("https://example.test/hook", saved.WebhookUrl);
        Assert.Equal(2, saved.BulkExportAlertThreshold);
        Assert.True(saved.BusinessHoursEnd > saved.BusinessHoursStart);

        var reloaded = await _da.GetGlbaSettingsAsync();
        Assert.Equal("Pacific Standard Time", reloaded.InstitutionTimeZone);
        Assert.True(reloaded.AlertsEnabled);

        // Leave alerts off for the rest of the suite (shared settings store).
        reloaded.AlertsEnabled = false;
        reloaded.WebhookUrl = "";
        await _da.SaveGlbaSettingsAsync(reloaded);
    }

    [Fact]
    public async Task SendTestGlbaAlert_ReturnsFalseWhenDisabled()
    {
        Assert.False(await _da.SendTestGlbaAlertAsync());
    }

    // -------------------------------------------------------------- insights

    [Fact]
    public async Task Insights_FlagUnownedSystems_AndZeroFillTrend()
    {
        await CreateSourceSystemAsync(); // active, no owner

        var insights = await _da.GetGlbaInsightsAsync(30);

        Assert.Equal(30, insights.EventsPerDay.Count);
        Assert.Equal(insights.EventsPerDay.Min(x => x.Date), insights.EventsPerDay.First().Date);
        Assert.Contains(insights.Items, x => x.Title.Contains("no data owner"));
    }
}
