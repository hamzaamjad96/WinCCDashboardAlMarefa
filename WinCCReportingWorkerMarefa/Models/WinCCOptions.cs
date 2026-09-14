namespace WinCCReportingWorkerMarefa.Models;

public class WinCCOptions
{
    public WinCCServerOptions Server1 { get; set; } = new();

    public WinCCServerOptions Server2 { get; set; } = new();
}

public class WinCCServerOptions
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;
}