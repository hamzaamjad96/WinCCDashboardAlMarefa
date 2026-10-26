namespace WinCCDashboard.Application.Abstractions.Reporting;

/// <summary>
/// A report parameter exactly as SSRS describes it for a deployed report.
/// </summary>
public sealed class ReportServerParameter
{
    public string Name { get; init; } = string.Empty;

    /// <summary>SSRS type name: String, Boolean, DateTime, Integer or Float.</summary>
    public string DataType { get; init; } = "String";

    public bool Nullable { get; init; }
    public bool AllowBlank { get; init; }
    public bool MultiValue { get; init; }

    /// <summary>False for internal parameters (never prompted).</summary>
    public bool PromptUser { get; init; } = true;

    public string? Prompt { get; init; }

    public bool DefaultValuesQueryBased { get; init; }
    public IReadOnlyList<string?> DefaultValues { get; init; } = Array.Empty<string?>();

    public bool ValidValuesQueryBased { get; init; }
    public IReadOnlyList<ReportParameterOption> ValidValues { get; init; } = Array.Empty<ReportParameterOption>();

    /// <summary>True when SSRS returned a default (including an explicit NULL default).</summary>
    public bool HasDefault => DefaultValues.Count > 0;
}

public sealed record ReportParameterOption(string Value, string Label);
