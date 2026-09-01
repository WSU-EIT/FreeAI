using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FreeGLBA;

// ============================================================================
// GLBA COMPLIANCE REPORT GENERATION
// Produces the actual report content (CSV detail export and PDF summary) for
// a ComplianceReport record, and stores the summary JSON in ReportData.
// ============================================================================

public partial interface IDataAccess
{
    /// <summary>Generates a CSV export of every access event in the report's period.</summary>
    Task<DataObjects.ComplianceReportExport?> GenerateComplianceReportCsvAsync(Guid reportId, string? generatedBy = null);

    /// <summary>Generates a PDF summary report for the report's period.</summary>
    Task<DataObjects.ComplianceReportExport?> GenerateComplianceReportPdfAsync(Guid reportId, string? generatedBy = null);

    /// <summary>
    /// Generates a PDF of one data subject's complete access history (a
    /// DSAR/audit-style export): every recorded access to that person's data,
    /// including the data-owner snapshot for each event.
    /// </summary>
    Task<DataObjects.ComplianceReportExport?> GenerateSubjectAccessHistoryPdfAsync(string subjectExternalId, string? generatedBy = null);
}

public partial class DataAccess
{
    #region Compliance Report Generation

    private sealed class ReportSourceData
    {
        public EFModels.EFModels.ComplianceReportItem Report = null!;
        public List<EFModels.EFModels.AccessEventItem> Events = new();
        public Dictionary<Guid, EFModels.EFModels.SourceSystemItem> Sources = new();
        public DateTime PeriodStartUtc;
        public DateTime PeriodEndUtc;
    }

    private async Task<ReportSourceData?> LoadReportSourceDataAsync(Guid reportId)
    {
        var report = await data.ComplianceReports.FindAsync(reportId);
        if (report == null) return null;

        var periodStartUtc = DateTime.SpecifyKind(report.PeriodStart.Date, DateTimeKind.Utc);
        var periodEndUtc = DateTime.SpecifyKind(report.PeriodEnd.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var events = await data.AccessEvents
            .AsNoTracking()
            .Where(x => x.AccessedAt >= periodStartUtc && x.AccessedAt <= periodEndUtc)
            .OrderBy(x => x.AccessedAt)
            .ToListAsync();

        var sourceIds = events.Select(x => x.SourceSystemId).Distinct().ToList();
        var sources = await data.SourceSystems
            .AsNoTracking()
            .Where(x => sourceIds.Contains(x.SourceSystemId))
            .ToDictionaryAsync(x => x.SourceSystemId);

        return new ReportSourceData {
            Report = report,
            Events = events,
            Sources = sources,
            PeriodStartUtc = periodStartUtc,
            PeriodEndUtc = periodEndUtc,
        };
    }

    /// <summary>
    /// Refreshes the stored statistics and summary JSON on the report record so
    /// the ComplianceReports table reflects what was actually generated.
    /// </summary>
    private async Task UpdateReportRecordAsync(ReportSourceData src, string? generatedBy)
    {
        var report = src.Report;
        report.GeneratedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(generatedBy)) {
            report.GeneratedBy = generatedBy;
        }
        report.TotalEvents = src.Events.Count;
        report.UniqueUsers = src.Events.Select(x => x.UserId).Distinct().Count();
        report.UniqueSubjects = src.Events.Where(x => x.SubjectId != "SYSTEM" && x.SubjectId != "BULK")
            .Select(x => x.SubjectId).Distinct().Count();

        var summary = new {
            generatedAt = report.GeneratedAt,
            periodStart = src.PeriodStartUtc,
            periodEnd = src.PeriodEndUtc,
            totalEvents = report.TotalEvents,
            uniqueUsers = report.UniqueUsers,
            uniqueSubjects = report.UniqueSubjects,
            byAccessType = src.Events.GroupBy(x => x.AccessType)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => string.IsNullOrEmpty(g.Key) ? "(none)" : g.Key, g => g.Count()),
            byDataCategory = src.Events.Where(x => !string.IsNullOrEmpty(x.DataCategory))
                .GroupBy(x => x.DataCategory)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count()),
        };
        report.ReportData = SerializeObject(summary);

        await data.SaveChangesAsync();
    }

    public async Task<DataObjects.ComplianceReportExport?> GenerateComplianceReportCsvAsync(Guid reportId, string? generatedBy = null)
    {
        var src = await LoadReportSourceDataAsync(reportId);
        if (src == null) return null;

        static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Contains('"') || value.Contains(',') || value.Contains('\n')
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("AccessedAt (UTC),ReceivedAt (UTC),SourceSystem,SourceEventId,UserId,UserName,UserEmail,UserDepartment,SubjectId,SubjectCount,SubjectType,DataCategory,AccessType,Purpose,IpAddress,DataOwnerName,DataOwnerEmail,DataOwnerDepartment");

        foreach (var evt in src.Events) {
            src.Sources.TryGetValue(evt.SourceSystemId, out var source);
            csv.Append(evt.AccessedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',');
            csv.Append(evt.ReceivedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',');
            csv.Append(Escape(source?.Name)).Append(',');
            csv.Append(Escape(evt.SourceEventId)).Append(',');
            csv.Append(Escape(evt.UserId)).Append(',');
            csv.Append(Escape(evt.UserName)).Append(',');
            csv.Append(Escape(evt.UserEmail)).Append(',');
            csv.Append(Escape(evt.UserDepartment)).Append(',');
            csv.Append(Escape(evt.SubjectId)).Append(',');
            csv.Append(evt.SubjectCount).Append(',');
            csv.Append(Escape(evt.SubjectType)).Append(',');
            csv.Append(Escape(evt.DataCategory)).Append(',');
            csv.Append(Escape(evt.AccessType)).Append(',');
            csv.Append(Escape(evt.Purpose)).Append(',');
            csv.Append(Escape(evt.IpAddress)).Append(',');
            csv.Append(Escape(evt.DataOwnerName)).Append(',');
            csv.Append(Escape(evt.DataOwnerEmail)).Append(',');
            csv.AppendLine(Escape(evt.DataOwnerDepartment));
        }

        await UpdateReportRecordAsync(src, generatedBy);

        return new DataObjects.ComplianceReportExport {
            FileName = $"glba-report-{src.Report.PeriodStart:yyyyMMdd}-{src.Report.PeriodEnd:yyyyMMdd}.csv",
            ContentType = "text/csv",
            Bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString()),
        };
    }

    public async Task<DataObjects.ComplianceReportExport?> GenerateComplianceReportPdfAsync(Guid reportId, string? generatedBy = null)
    {
        var src = await LoadReportSourceDataAsync(reportId);
        if (src == null) return null;

        var report = src.Report;
        var events = src.Events;

        var byAccessType = events.GroupBy(x => string.IsNullOrEmpty(x.AccessType) ? "(none)" : x.AccessType)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count).ToList();
        var byCategory = events.Where(x => !string.IsNullOrEmpty(x.DataCategory))
            .GroupBy(x => x.DataCategory)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count).ToList();
        var topAccessors = events.GroupBy(x => x.UserId)
            .Select(g => new {
                UserId = g.Key,
                UserName = g.OrderByDescending(x => x.AccessedAt).First().UserName,
                Department = g.OrderByDescending(x => x.AccessedAt).First().UserDepartment,
                Total = g.Count(),
                Subjects = g.Select(x => x.SubjectId).Distinct().Count(),
                Exports = g.Count(x => x.AccessType == "Export" || x.AccessType == "Download"),
            })
            .OrderByDescending(x => x.Total).Take(10).ToList();
        var bySource = events.GroupBy(x => x.SourceSystemId)
            .Select(g => new { SourceSystemId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();
        var totalEvents = events.Count;
        var uniqueUsers = events.Select(x => x.UserId).Distinct().Count();
        var uniqueSubjects = events.Where(x => x.SubjectId != "SYSTEM" && x.SubjectId != "BULK")
            .Select(x => x.SubjectId).Distinct().Count();
        var exportEvents = events.Count(x => x.AccessType == "Export" || x.AccessType == "Download");

        static IContainer HeaderCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Darken1)
            .PaddingVertical(4).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.SemiBold().FontSize(9));

        static IContainer BodyCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.FontSize(9));

        var reportType = string.IsNullOrEmpty(report.ReportType) ? "Access Summary" : report.ReportType;
        var generatedAt = DateTime.UtcNow;

        var pdfBytes = Document.Create(document => {
            document.Page(page => {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Column(col => {
                    col.Item().Text("GLBA Compliance Report").FontSize(20).SemiBold();
                    col.Item().Text($"{reportType} · {report.PeriodStart:MMMM d, yyyy} – {report.PeriodEnd:MMMM d, yyyy}")
                        .FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(2).Text($"Generated {generatedAt:MMMM d, yyyy HH:mm} UTC" +
                        (string.IsNullOrWhiteSpace(generatedBy) ? "" : $" by {generatedBy}"))
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(6).BorderBottom(2).BorderColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingVertical(12).Column(col => {
                    col.Spacing(14);

                    // Summary statistics
                    col.Item().Row(row => {
                        void Stat(string value, string label)
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c => {
                                c.Item().AlignCenter().Text(value).FontSize(16).SemiBold();
                                c.Item().AlignCenter().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        }
                        Stat(totalEvents.ToString("N0"), "Access events");
                        Stat(uniqueUsers.ToString("N0"), "Unique accessors");
                        Stat(uniqueSubjects.ToString("N0"), "Unique data subjects");
                        Stat(exportEvents.ToString("N0"), "Exports / downloads");
                    });

                    // Breakdown tables side by side
                    col.Item().Row(row => {
                        row.Spacing(16);
                        row.RelativeItem().Column(c => {
                            c.Item().Text("By Access Type").FontSize(11).SemiBold();
                            c.Item().PaddingTop(4).Table(t => {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(1); });
                                t.Header(h => {
                                    h.Cell().Element(HeaderCell).Text("Type");
                                    h.Cell().Element(HeaderCell).AlignRight().Text("Events");
                                });
                                foreach (var itm in byAccessType) {
                                    t.Cell().Element(BodyCell).Text(itm.Label);
                                    t.Cell().Element(BodyCell).AlignRight().Text(itm.Count.ToString("N0"));
                                }
                            });
                        });
                        row.RelativeItem().Column(c => {
                            c.Item().Text("By Data Category").FontSize(11).SemiBold();
                            c.Item().PaddingTop(4).Table(t => {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(1); });
                                t.Header(h => {
                                    h.Cell().Element(HeaderCell).Text("Category");
                                    h.Cell().Element(HeaderCell).AlignRight().Text("Events");
                                });
                                foreach (var itm in byCategory) {
                                    t.Cell().Element(BodyCell).Text(itm.Label);
                                    t.Cell().Element(BodyCell).AlignRight().Text(itm.Count.ToString("N0"));
                                }
                            });
                        });
                    });

                    // Top accessors
                    col.Item().Column(c => {
                        c.Item().Text("Top Accessors").FontSize(11).SemiBold();
                        c.Item().PaddingTop(4).Table(t => {
                            t.ColumnsDefinition(cd => {
                                cd.RelativeColumn(2); cd.RelativeColumn(3); cd.RelativeColumn(3);
                                cd.RelativeColumn(1); cd.RelativeColumn(1); cd.RelativeColumn(1);
                            });
                            t.Header(h => {
                                h.Cell().Element(HeaderCell).Text("User ID");
                                h.Cell().Element(HeaderCell).Text("Name");
                                h.Cell().Element(HeaderCell).Text("Department");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Events");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Subjects");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Exports");
                            });
                            foreach (var accessor in topAccessors) {
                                t.Cell().Element(BodyCell).Text(accessor.UserId);
                                t.Cell().Element(BodyCell).Text(accessor.UserName);
                                t.Cell().Element(BodyCell).Text(accessor.Department);
                                t.Cell().Element(BodyCell).AlignRight().Text(accessor.Total.ToString("N0"));
                                t.Cell().Element(BodyCell).AlignRight().Text(accessor.Subjects.ToString("N0"));
                                t.Cell().Element(BodyCell).AlignRight().Text(accessor.Exports.ToString("N0"));
                            }
                        });
                    });

                    // Source systems and data owners
                    col.Item().Column(c => {
                        c.Item().Text("Source Systems and Data Owners").FontSize(11).SemiBold();
                        c.Item().PaddingTop(4).Table(t => {
                            t.ColumnsDefinition(cd => {
                                cd.RelativeColumn(3); cd.RelativeColumn(3); cd.RelativeColumn(3); cd.RelativeColumn(1);
                            });
                            t.Header(h => {
                                h.Cell().Element(HeaderCell).Text("System");
                                h.Cell().Element(HeaderCell).Text("Current Data Owner");
                                h.Cell().Element(HeaderCell).Text("Owner Department");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Events");
                            });
                            foreach (var srcRow in bySource) {
                                src.Sources.TryGetValue(srcRow.SourceSystemId, out var system);
                                t.Cell().Element(BodyCell).Text(system?.Name ?? "(deleted system)");
                                t.Cell().Element(BodyCell).Text(string.IsNullOrEmpty(system?.DataOwnerName) ? "Not recorded" : system!.DataOwnerName);
                                t.Cell().Element(BodyCell).Text(system?.DataOwnerDepartment ?? "");
                                t.Cell().Element(BodyCell).AlignRight().Text(srcRow.Count.ToString("N0"));
                            }
                        });
                    });

                    col.Item().PaddingTop(4).Text(
                        "Access events record who accessed protected financial information, when, how, and for what stated purpose, " +
                        "with a snapshot of the responsible data owner at the time of access. Generated by FreeGLBA in support of the " +
                        "FTC Safeguards Rule requirement to monitor and log the activity of authorized users (16 CFR 314.4(c)(8)).")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1)).Text(t => {
                    t.Span("FreeGLBA Compliance Report · Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();

        await UpdateReportRecordAsync(src, generatedBy);

        return new DataObjects.ComplianceReportExport {
            FileName = $"glba-report-{report.PeriodStart:yyyyMMdd}-{report.PeriodEnd:yyyyMMdd}.pdf",
            ContentType = "application/pdf",
            Bytes = pdfBytes,
        };
    }

    public async Task<DataObjects.ComplianceReportExport?> GenerateSubjectAccessHistoryPdfAsync(string subjectExternalId, string? generatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(subjectExternalId)) return null;
        subjectExternalId = subjectExternalId.Trim();

        var subject = await data.DataSubjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExternalId == subjectExternalId);
        if (subject == null) return null;

        // Every event that touched this subject: direct matches plus membership
        // in multi-subject (JSON list) events. Capped to keep the PDF bounded.
        const int maxEvents = 2000;
        var events = await data.AccessEvents
            .AsNoTracking()
            .Where(x => x.SubjectId == subjectExternalId || (x.SubjectCount > 1 && x.SubjectIds.Contains(subjectExternalId)))
            .OrderByDescending(x => x.AccessedAt)
            .Take(maxEvents)
            .ToListAsync();

        var sourceIds = events.Select(x => x.SourceSystemId).Distinct().ToList();
        var sources = await data.SourceSystems
            .AsNoTracking()
            .Where(x => sourceIds.Contains(x.SourceSystemId))
            .ToDictionaryAsync(x => x.SourceSystemId);

        var generatedAt = DateTime.UtcNow;
        var uniqueUsers = events.Select(x => x.UserId).Distinct().Count();
        var exportCount = events.Count(x => x.AccessType == "Export" || x.AccessType == "Download");

        static IContainer HeaderCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Darken1)
            .PaddingVertical(4).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.SemiBold().FontSize(8));

        static IContainer BodyCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.FontSize(8));

        var pdfBytes = Document.Create(document => {
            document.Page(page => {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                page.Header().Column(col => {
                    col.Item().Text("Data Subject Access History").FontSize(18).SemiBold();
                    col.Item().Text($"Subject {subject.ExternalId} ({(string.IsNullOrEmpty(subject.SubjectType) ? "unknown type" : subject.SubjectType)})")
                        .FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(2).Text($"Generated {generatedAt:MMMM d, yyyy HH:mm} UTC" +
                        (string.IsNullOrWhiteSpace(generatedBy) ? "" : $" by {generatedBy}"))
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(6).BorderBottom(2).BorderColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingVertical(12).Column(col => {
                    col.Spacing(12);

                    col.Item().Row(row => {
                        void Stat(string value, string label)
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c => {
                                c.Item().AlignCenter().Text(value).FontSize(14).SemiBold();
                                c.Item().AlignCenter().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        }
                        Stat(events.Count.ToString("N0"), "Access events");
                        Stat(uniqueUsers.ToString("N0"), "Unique accessors");
                        Stat(exportCount.ToString("N0"), "Exports / downloads");
                        Stat(events.Count > 0 ? events.Min(x => x.AccessedAt).ToString("MMM d, yyyy") : "-", "Earliest access shown");
                    });

                    if (events.Count >= maxEvents) {
                        col.Item().Text($"Showing the most recent {maxEvents:N0} events; older history exists in the system.")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().Table(t => {
                        t.ColumnsDefinition(cd => {
                            cd.ConstantColumn(78); // when
                            cd.RelativeColumn(2);  // who
                            cd.RelativeColumn(2);  // dept
                            cd.ConstantColumn(48); // type
                            cd.RelativeColumn(2);  // category
                            cd.RelativeColumn(3);  // purpose
                            cd.RelativeColumn(2);  // source
                            cd.RelativeColumn(2);  // owner at time
                        });
                        t.Header(h => {
                            h.Cell().Element(HeaderCell).Text("When (UTC)");
                            h.Cell().Element(HeaderCell).Text("Accessed By");
                            h.Cell().Element(HeaderCell).Text("Department");
                            h.Cell().Element(HeaderCell).Text("Type");
                            h.Cell().Element(HeaderCell).Text("Category");
                            h.Cell().Element(HeaderCell).Text("Purpose");
                            h.Cell().Element(HeaderCell).Text("Source System");
                            h.Cell().Element(HeaderCell).Text("Data Owner (then)");
                        });
                        foreach (var evt in events) {
                            sources.TryGetValue(evt.SourceSystemId, out var source);
                            t.Cell().Element(BodyCell).Text(evt.AccessedAt.ToString("yyyy-MM-dd HH:mm"));
                            t.Cell().Element(BodyCell).Text(string.IsNullOrEmpty(evt.UserName) ? evt.UserId : $"{evt.UserName} ({evt.UserId})");
                            t.Cell().Element(BodyCell).Text(evt.UserDepartment);
                            t.Cell().Element(BodyCell).Text(evt.AccessType);
                            t.Cell().Element(BodyCell).Text(evt.DataCategory);
                            t.Cell().Element(BodyCell).Text(evt.Purpose);
                            t.Cell().Element(BodyCell).Text(source?.Name ?? "");
                            t.Cell().Element(BodyCell).Text(evt.DataOwnerName);
                        }
                    });

                    col.Item().PaddingTop(4).Text(
                        "Complete recorded history of access to this individual's protected financial information, " +
                        "including the responsible data owner at the time of each access. Generated by FreeGLBA in " +
                        "support of the FTC Safeguards Rule (16 CFR 314.4(c)(8)).")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1)).Text(t => {
                    t.Span($"Subject {subject.ExternalId} · Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();

        return new DataObjects.ComplianceReportExport {
            FileName = $"glba-subject-{SafeFileName(subject.ExternalId)}-{generatedAt:yyyyMMdd}.pdf",
            ContentType = "application/pdf",
            Bytes = pdfBytes,
        };
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    #endregion
}
