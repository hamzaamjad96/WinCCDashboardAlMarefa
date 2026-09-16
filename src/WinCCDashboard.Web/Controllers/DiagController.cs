using Microsoft.AspNetCore.Mvc;
using WinCCDashboard.Application.Abstractions.Pdf;
using WinCCDashboard.Domain.Entities;

namespace WinCCCustomDashboardMarefa.Controllers;

public class DiagController : Controller
{
    private readonly IAlarmHistoryPdfGenerator _generator;

    public DiagController(IAlarmHistoryPdfGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet]
    public IActionResult TestPdf()
    {
        var alarms = new List<AlarmEvent>
        {
            new() { Id = 1, AlarmName = "Test1", RaiseTime = DateTime.UtcNow, Priority = 1, SourceServer = "SRV-01", SyncedAt = DateTime.UtcNow },
            new() { Id = 2, AlarmName = "Test2", RaiseTime = DateTime.UtcNow, Priority = 4, SourceServer = "SRV-02", SyncedAt = DateTime.UtcNow },
            new() { Id = 3, AlarmName = "Test3", RaiseTime = DateTime.UtcNow, Priority = 12, SourceServer = "SRV-01", SyncedAt = DateTime.UtcNow }
        };

        var bytes = _generator.Generate(alarms);

        // Sirf size return karo, file MAT bhejo
        return Ok(new
        {
            success = true,
            bytes = bytes.Length,
            message = "PDF generated but NOT sent as file"
        });
    }
}