using System.Text.Json.Serialization;

namespace WinCCReportingWorkerMarefa.Models.GraphQL;

public class LoggedAlarmResponse
{
    [JsonPropertyName("data")]
    public LoggedAlarmData? Data { get; set; }
}

public class LoggedAlarmData
{
    [JsonPropertyName("loggedAlarms")]
    public List<LoggedAlarmDto> LoggedAlarms { get; set; } = [];
}

public class LoggedAlarmDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("raiseTime")]
    public DateTime RaiseTime { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }
}