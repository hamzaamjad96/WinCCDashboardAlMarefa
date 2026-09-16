using PdfSharp.Fonts;

namespace WinCCCustomDashboardMarefa.Pdf;

public class WindowsFontResolver : IFontResolver
{
    private static readonly string FontsDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    public string DefaultFontName => "Arial";

    public byte[]? GetFont(string faceName)
    {
        // faceName format: "Arial#Bold" / "Courier New#Regular" etc.
        var parts = faceName.Split('#');
        var family = parts[0];
        var style = parts.Length > 1 ? parts[1] : "Regular";

        var fileName = GetFileName(family, style);
        if (fileName is null) return null;

        var path = Path.Combine(FontsDir, fileName);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    public FontResolverInfo? ResolveTypeface(
        string familyName, bool isBold, bool isItalic)
    {
        var style = (isBold, isItalic) switch
        {
            (true, true) => "BoldItalic",
            (true, false) => "Bold",
            (false, true) => "Italic",
            _ => "Regular"
        };

        // Agar requested font mil jaye to wahi use karo
        if (GetFileName(familyName, style) is not null)
            return new FontResolverInfo($"{familyName}#{style}");

        // Warna Arial pe fallback
        return new FontResolverInfo($"Arial#{style}");
    }

    private static string? GetFileName(string family, string style)
        => (family.ToLowerInvariant(), style) switch
        {
            ("arial", "Regular") => "arial.ttf",
            ("arial", "Bold") => "arialbd.ttf",
            ("arial", "Italic") => "ariali.ttf",
            ("arial", "BoldItalic") => "arialbi.ttf",

            ("courier new", "Regular") => "cour.ttf",
            ("courier new", "Bold") => "courbd.ttf",
            ("courier new", "Italic") => "couri.ttf",
            ("courier new", "BoldItalic") => "courbi.ttf",

            ("times new roman", "Regular") => "times.ttf",
            ("times new roman", "Bold") => "timesbd.ttf",
            ("times new roman", "Italic") => "timesi.ttf",
            ("times new roman", "BoldItalic") => "timesbi.ttf",

            ("segoe ui", "Regular") => "segoeui.ttf",
            ("segoe ui", "Bold") => "segoeuib.ttf",
            ("segoe ui", "Italic") => "segoeuii.ttf",
            ("segoe ui", "BoldItalic") => "segoeuiz.ttf",

            ("verdana", "Regular") => "verdana.ttf",
            ("verdana", "Bold") => "verdanab.ttf",
            ("verdana", "Italic") => "verdanai.ttf",
            ("verdana", "BoldItalic") => "verdanaz.ttf",

            _ => null
        };
}