using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using WinCCCustomDashboardMarefa.Models;

namespace WinCCCustomDashboardMarefa.Pdf;

public class AlarmHistoryPdfService
{
    private static readonly object PdfLock = new();

    public byte[] Generate(List<AlarmEvent> alarms)
    {
        lock (PdfLock)
        {
            var document = new Document();

            document.Info.Title = "WinCC Unified Alarm History";
            document.Info.Author = "WinCC Reporting Dashboard";

            DefineStyles(document);

            var section = document.AddSection();

            section.PageSetup.Orientation = Orientation.Landscape;
            section.PageSetup.TopMargin = Unit.FromCentimeter(1.2);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1.2);
            section.PageSetup.LeftMargin = Unit.FromCentimeter(1.2);
            section.PageSetup.RightMargin = Unit.FromCentimeter(1.2);

            var title = section.AddParagraph();
            title.Style = "ReportTitle";
            title.AddText("WinCC Unified");

            var subtitle = section.AddParagraph();
            subtitle.Style = "ReportSubtitle";
            subtitle.AddText("Alarm History Report");

            section.AddParagraph();

            var info = section.AddTable();
            info.Borders.Width = 0;
            info.AddColumn(Unit.FromCentimeter(4));
            info.AddColumn(Unit.FromCentimeter(10));

            AddInfoRow(info, "Generated At",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            AddInfoRow(info, "Total Records", alarms.Count.ToString());

            section.AddParagraph();

            var table = section.AddTable();
            table.Borders.Width = 0.5;
            table.Borders.Color = Colors.LightGray;

            table.AddColumn(Unit.FromCentimeter(1.0));
            table.AddColumn(Unit.FromCentimeter(7.0));
            table.AddColumn(Unit.FromCentimeter(4.0));
            table.AddColumn(Unit.FromCentimeter(2.0));
            table.AddColumn(Unit.FromCentimeter(3.0));
            table.AddColumn(Unit.FromCentimeter(4.0));

            var header = table.AddRow();
            header.HeadingFormat = true;
            header.Format.Font.Bold = true;
            header.Format.Font.Size = 8;

            AddHeader(header.Cells[0], "#");
            AddHeader(header.Cells[1], "Alarm Name");
            AddHeader(header.Cells[2], "Raise Time");
            AddHeader(header.Cells[3], "Priority");
            AddHeader(header.Cells[4], "Source Server");
            AddHeader(header.Cells[5], "Synced At");

            var index = 1;
            foreach (var alarm in alarms)
            {
                var row = table.AddRow();
                row.Format.Font.Size = 7;

                row.Cells[0].AddParagraph(index.ToString());
                row.Cells[1].AddParagraph(alarm.AlarmName);
                row.Cells[2].AddParagraph(
                    alarm.RaiseTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                row.Cells[3].AddParagraph(
                    alarm.Priority?.ToString() ?? "-");
                row.Cells[4].AddParagraph(alarm.SourceServer);
                row.Cells[5].AddParagraph(
                    alarm.SyncedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                index++;
            }

            var footer = section.Footers.Primary;
            var footerParagraph = footer.AddParagraph();
            footerParagraph.Format.Alignment = ParagraphAlignment.Center;
            footerParagraph.Format.Font.Size = 8;
            footerParagraph.AddText("WinCC Reporting Dashboard | Page ");
            footerParagraph.AddPageField();
            footerParagraph.AddText(" of ");
            footerParagraph.AddNumPagesField();

            // NOTE: PdfDocumentRenderer does not implement IDisposable,
            // so we can't use "using var" — lock + GC handle cleanup
            var renderer = new PdfDocumentRenderer
            {
                Document = document
            };

            renderer.RenderDocument();

            using var stream = new MemoryStream();
            renderer.PdfDocument.Save(stream, false);

            return stream.ToArray();
        }
    }

    private static void DefineStyles(Document document)
    {
        var normal = document.Styles["Normal"]
    ?? throw new InvalidOperationException("Normal style not found.");

        normal.Font.Name = "Arial";
        normal.Font.Size = 9;

        var title = document.Styles.AddStyle("ReportTitle", "Normal");
        title.Font.Name = "Arial";
        title.Font.Size = 20;
        title.Font.Bold = true;

        var subtitle = document.Styles.AddStyle("ReportSubtitle", "Normal");
        subtitle.Font.Name = "Arial";
        subtitle.Font.Size = 13;
        subtitle.Font.Bold = true;
    }

    private static void AddHeader(Cell cell, string text)
    {
        cell.AddParagraph(text);
    }

    private static void AddInfoRow(Table table, string label, string value)
    {
        var row = table.AddRow();
        row.Cells[0].AddParagraph(label);
        row.Cells[0].Format.Font.Bold = true;
        row.Cells[1].AddParagraph(value);
    }
}