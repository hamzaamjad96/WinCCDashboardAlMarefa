using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WinCCDashboard.Application.Abstractions.Pdf;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Infrastructure.Pdf;

public class DailySummaryPdfGenerator : IDailySummaryPdfGenerator
{
    public byte[] Generate(List<DailyAlarmSummary> data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                // ---------- HEADER ----------
                page.Header().Column(col =>
                {
                    col.Item().Text("WinCC Unified")
                        .FontSize(18).Bold();
                    col.Item().Text("Daily Alarm Summary")
                        .FontSize(12).Bold();
                    col.Item().PaddingTop(6)
                        .Text($"Generated At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    col.Item().Text($"Total Days: {data.Count}");
                });

                // ---------- CONTENT TABLE ----------
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.4f); // Date
                        columns.RelativeColumn(1);    // Total
                        columns.RelativeColumn(1);    // High Priority
                        columns.RelativeColumn(1);    // P1
                        columns.RelativeColumn(1);    // P4
                        columns.RelativeColumn(1);    // P12
                        columns.RelativeColumn(1);    // SRV-01
                        columns.RelativeColumn(1);    // SRV-02
                    });

                    table.Header(header =>
                    {
                        var headerBg = Colors.Grey.Lighten2;

                        header.Cell().Background(headerBg).Padding(4).Text("Date").Bold();
                        header.Cell().Background(headerBg).Padding(4).Text("Total").Bold();
                        header.Cell().Background(headerBg).Padding(4).Text("High Priority").Bold();
                        header.Cell().Background(headerBg).Padding(4).Text("Priority 1").Bold();
                        header.Cell().Background(headerBg).Padding(4).Text("Priority 4").Bold();
                        header.Cell().Background(headerBg).Padding(4).Text("Priority 12").Bold();
                        header.Cell().Background(headerBg).Padding(4).Text("SRV-01").Bold();
                        header.Cell().Background(headerBg).Padding(4).Text("SRV-02").Bold();
                    });

                    var index = 0;
                    foreach (var item in data)
                    {
                        var bg = index % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                        table.Cell().Background(bg).Padding(4).Text(item.AlarmDate.ToString("yyyy-MM-dd"));
                        table.Cell().Background(bg).Padding(4).Text(item.TotalAlarms.ToString());
                        table.Cell().Background(bg).Padding(4).Text(item.HighPriorityAlarms.ToString());
                        table.Cell().Background(bg).Padding(4).Text(item.Priority1Alarms.ToString());
                        table.Cell().Background(bg).Padding(4).Text(item.Priority4Alarms.ToString());
                        table.Cell().Background(bg).Padding(4).Text(item.Priority12Alarms.ToString());
                        table.Cell().Background(bg).Padding(4).Text(item.Srv01Alarms.ToString());
                        table.Cell().Background(bg).Padding(4).Text(item.Srv02Alarms.ToString());

                        index++;
                    }
                });

                // ---------- FOOTER ----------
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("WinCC Reporting Dashboard | Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}