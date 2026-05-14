using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FreeA11yChecker.Server.Controllers;

public partial class DataController
{
    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/ExportAuditCsv")]
    public async Task<ActionResult> ExportAuditCsv([FromBody] Guid SiteId)
    {
        string csv = await da.ExportAuditCsv(SiteId);
        byte[] csvBytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(csvBytes, "text/csv", "audit-report.csv");
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/ExportAuditPdf")]
    public async Task<ActionResult> ExportAuditPdf([FromBody] Guid SiteId)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        DataObjects.AuditReport report = await da.GenerateAuditReport(SiteId, CurrentUser);
        if (report == null) { return BadRequest("Report generation failed — site not found or no data available."); }

        var document = Document.Create(container => {
            container.Page(page => {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col => {
                    col.Item().Text("Accessibility Audit Report").FontSize(18).Bold();
                    col.Item().Text(report.SiteName + " — " + report.SiteUrl).FontSize(12);
                    col.Item().Text("Generated: " + report.GeneratedAt.ToString("yyyy-MM-dd HH:mm UTC")).FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingBottom(10);
                });

                page.Content().Column(col => {
                    // Summary
                    col.Item().Text("Summary").FontSize(14).Bold();
                    col.Item().Text("Evaluated: " + report.EvaluatedCount + " / " + report.TotalCriteria);
                    col.Item().Text("Supports: " + report.SupportsCount);
                    col.Item().Text("Does Not Support: " + report.DoesNotSupportCount);
                    col.Item().Text("Not Evaluated: " + report.NotEvaluatedCount);
                    col.Item().PaddingBottom(15);

                    // Criteria table
                    col.Item().Text("WCAG 2.2 AA Criteria").FontSize(14).Bold();
                    col.Item().PaddingBottom(5);

                    col.Item().Table(table => {
                        table.ColumnsDefinition(c => {
                            c.ConstantColumn(55);
                            c.RelativeColumn(3);
                            c.ConstantColumn(35);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                        });

                        // Header
                        table.Header(h => {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Criterion").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Name").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Level").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Conformance").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Source").Bold();
                        });

                        // Data rows
                        foreach (DataObjects.WcagCriterionStatus c in report.Criteria.OrderBy(x => x.Criterion)) {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(c.Criterion);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(c.Name);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(c.Level);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(c.ConformanceLevel);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(c.Source);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t => {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        });

        byte[] pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", "audit-report-" + report.SiteName + ".pdf");
    }
}
