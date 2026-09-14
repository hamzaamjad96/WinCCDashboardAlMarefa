using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using WinCCCustomDashboardMarefa.Models;

namespace WinCCCustomDashboardMarefa.Pdf;

public class DailyAlarmSummaryPdfService
{
    public byte[] Generate(List<DailyAlarmSummary> data)
    {
        var document = new Document();

        document.Info.Title = "Daily Alarm Summary";
        document.Info.Author = "WinCC Reporting Dashboard";

        DefineStyles(document);

        var section = document.AddSection();

        section.PageSetup.Orientation =
            Orientation.Landscape;

        section.PageSetup.TopMargin =
            Unit.FromCentimeter(1.2);

        section.PageSetup.BottomMargin =
            Unit.FromCentimeter(1.2);

        section.PageSetup.LeftMargin =
            Unit.FromCentimeter(1.2);

        section.PageSetup.RightMargin =
            Unit.FromCentimeter(1.2);


        // Title

        var title = section.AddParagraph();

        title.Style = "ReportTitle";

        title.AddText("WinCC Unified");


        var subtitle = section.AddParagraph();

        subtitle.Style = "ReportSubtitle";

        subtitle.AddText("Daily Alarm Summary");


        section.AddParagraph();


        // Report information

        var info = section.AddTable();

        info.Borders.Width = 0;

        info.AddColumn(
            Unit.FromCentimeter(4));

        info.AddColumn(
            Unit.FromCentimeter(10));


        AddInfoRow(
            info,
            "Generated At",
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss"));


        AddInfoRow(
            info,
            "Total Days",
            data.Count.ToString());


        section.AddParagraph();


        // Table

        var table = section.AddTable();

        table.Borders.Width = 0.5;

        table.Borders.Color =
            Colors.LightGray;


        table.AddColumn(
            Unit.FromCentimeter(2.8));

        table.AddColumn(
            Unit.FromCentimeter(2.5));

        table.AddColumn(
            Unit.FromCentimeter(2.5));

        table.AddColumn(
            Unit.FromCentimeter(2.2));

        table.AddColumn(
            Unit.FromCentimeter(2.2));

        table.AddColumn(
            Unit.FromCentimeter(2.4));

        table.AddColumn(
            Unit.FromCentimeter(2.4));

        table.AddColumn(
            Unit.FromCentimeter(2.4));


        var header = table.AddRow();

        header.HeadingFormat = true;

        header.Format.Font.Bold = true;

        header.Format.Font.Size = 8;


        AddHeader(
            header.Cells[0],
            "Date");

        AddHeader(
            header.Cells[1],
            "Total");

        AddHeader(
            header.Cells[2],
            "High Priority");

        AddHeader(
            header.Cells[3],
            "Priority 1");

        AddHeader(
            header.Cells[4],
            "Priority 4");

        AddHeader(
            header.Cells[5],
            "Priority 12");

        AddHeader(
            header.Cells[6],
            "SRV-01");

        AddHeader(
            header.Cells[7],
            "SRV-02");


        foreach (var item in data)
        {
            var row = table.AddRow();

            row.Format.Font.Size = 8;


            row.Cells[0].AddParagraph(
                item.AlarmDate.ToString(
                    "yyyy-MM-dd"));


            row.Cells[1].AddParagraph(
                item.TotalAlarms.ToString());


            row.Cells[2].AddParagraph(
                item.HighPriorityAlarms.ToString());


            row.Cells[3].AddParagraph(
                item.Priority1Alarms.ToString());


            row.Cells[4].AddParagraph(
                item.Priority4Alarms.ToString());


            row.Cells[5].AddParagraph(
                item.Priority12Alarms.ToString());


            row.Cells[6].AddParagraph(
                item.Srv01Alarms.ToString());


            row.Cells[7].AddParagraph(
                item.Srv02Alarms.ToString());
        }


        // Footer

        var footer =
            section.Footers.Primary;

        var footerParagraph =
            footer.AddParagraph();


        footerParagraph.Format.Alignment =
            ParagraphAlignment.Center;


        footerParagraph.Format.Font.Size = 8;


        footerParagraph.AddText(
            "WinCC Reporting Dashboard | Page ");


        footerParagraph.AddPageField();


        footerParagraph.AddText(" of ");


        footerParagraph.AddNumPagesField();


        // Render

        var renderer =
            new PdfDocumentRenderer
            {
                Document = document
            };


        renderer.RenderDocument();


        using var stream =
            new MemoryStream();


        renderer.PdfDocument.Save(
            stream,
            false);


        return stream.ToArray();
    }


    private static void DefineStyles(
        Document document)
    {
        var normal =
            document.Styles["Normal"];

        normal.Font.Name = "Arial";

        normal.Font.Size = 9;


        var title =
            document.Styles.AddStyle(
                "ReportTitle",
                "Normal");

        title.Font.Name = "Arial";

        title.Font.Size = 20;

        title.Font.Bold = true;


        var subtitle =
            document.Styles.AddStyle(
                "ReportSubtitle",
                "Normal");

        subtitle.Font.Name = "Arial";

        subtitle.Font.Size = 13;

        subtitle.Font.Bold = true;
    }


    private static void AddHeader(
        Cell cell,
        string text)
    {
        cell.AddParagraph(text);
    }


    private static void AddInfoRow(
        Table table,
        string label,
        string value)
    {
        var row = table.AddRow();

        row.Cells[0]
            .AddParagraph(label);

        row.Cells[0]
            .Format.Font.Bold = true;

        row.Cells[1]
            .AddParagraph(value);
    }
}