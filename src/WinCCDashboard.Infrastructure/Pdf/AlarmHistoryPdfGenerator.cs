using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WinCCDashboard.Application.Abstractions.Pdf;
using WinCCDashboard.Domain.Entities;

namespace WinCCDashboard.Infrastructure.Pdf;

public class AlarmHistoryPdfGenerator : IAlarmHistoryPdfGenerator
{
    public byte[] Generate(List<AlarmEvent> alarms)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                // ---------- HEADER ----------
                page.Header().Column(col =>
                {
                    col.Item().Text("WinCC Unified")
                        .FontSize(18).Bold();
                    col.Item().Text("Alarm History Report")
                        .FontSize(12).Bold();
                    col.Item().PaddingTop(6)
                        .Text($"Generated At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    col.Item()
                        .Text($"Total Records: {alarms.Count}");
                });

                // ---------- CONTENT TABLE ----------
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);     // #
                        columns.RelativeColumn(3);      // Alarm Name
                        columns.RelativeColumn(2);      // Raise Time
                        columns.ConstantColumn(50);     // Priority
                        columns.RelativeColumn(1.5f);   // Source Server
                        columns.RelativeColumn(2);      // Synced At
                    });

                    // Table header
                    table.Header(header =>
                    {
                        var headerBg = Colors.Grey.Lighten2;

                        header.Cell().Background(headerBg).Padding(3).Text("#").Bold();
                        header.Cell().Background(headerBg).Padding(3).Text("Alarm Name").Bold();
                        header.Cell().Background(headerBg).Padding(3).Text("Raise Time").Bold();
                        header.Cell().Background(headerBg).Padding(3).Text("Priority").Bold();
                        header.Cell().Background(headerBg).Padding(3).Text("Source Server").Bold();
                        header.Cell().Background(headerBg).Padding(3).Text("Synced At").Bold();
                    });

                    var index = 1;
                    foreach (var alarm in alarms)
                    {
                        var bg = index % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                        table.Cell().Background(bg).Padding(3).Text(index.ToString());
                        table.Cell().Background(bg).Padding(3).Text(alarm.AlarmName);
                        table.Cell().Background(bg).Padding(3)
                            .Text(alarm.RaiseTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        table.Cell().Background(bg).Padding(3)
                            .Text(alarm.Priority?.ToString() ?? "-");
                        table.Cell().Background(bg).Padding(3)
                            .Text(alarm.SourceServer);
                        table.Cell().Background(bg).Padding(3)
                            .Text(alarm.SyncedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));

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