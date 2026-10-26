namespace WinCCDashboard.Application.Reports;

/// <summary>
/// Everything the browser needs to draw the filter panel for one report.
/// Built from the report's real SSRS parameters, so a report only shows
/// the filters it actually supports.
/// </summary>
public sealed class ReportFilterDefinition
{
    public string ReportKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public List<ReportFilterField> Fields { get; init; } = new();

    /// <summary>Short notes shown under the filters (e.g. time zone).</summary>
    public List<string> Notes { get; init; } = new();
}

public sealed class ReportFilterField
{
    /// <summary>SSRS parameter name; also the query-string name.</summary>
    public string Name { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;

    /// <summary>date | datetime | number | text | select | multiselect | checkbox</summary>
    public string Control { get; init; } = "text";

    /// <summary>"from" or "to" for the two halves of a date range, otherwise null.</summary>
    public string? Role { get; init; }

    public bool Required { get; init; }
    public string? DefaultValue { get; init; }
    public List<string> DefaultValues { get; init; } = new();

    /// <summary>Label of the empty choice in a dropdown ("All servers"); null when a value is mandatory.</summary>
    public string? EmptyOptionLabel { get; init; }
    public List<ReportFilterOption> Options { get; init; } = new();

    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
    public decimal? Step { get; init; }
    public int? MaxLength { get; init; }
    public string? Placeholder { get; init; }
    public string? Help { get; init; }
}

public sealed record ReportFilterOption(string Value, string Label);

/// <summary>Result of turning user filter values into SSRS parameters.</summary>
public sealed class ReportParameterBuildResult
{
    public List<KeyValuePair<string, string?>> Parameters { get; } = new();

    /// <summary>Validation errors keyed by parameter name ("" for form-level errors).</summary>
    public Dictionary<string, List<string>> Errors { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsValid => Errors.Count == 0;

    internal void AddError(string field, string message)
    {
        if (!Errors.TryGetValue(field, out var list))
            Errors[field] = list = new List<string>();
        list.Add(message);
    }
}
