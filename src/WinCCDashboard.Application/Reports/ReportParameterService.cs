using System.Globalization;
using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Application.Abstractions.Reporting;
using WinCCDashboard.Domain.Entities;
// Alias avoids any clash with an older WinCCDashboard.Application.Reports.ReportDefinition class.
using ReportEntity = WinCCDashboard.Domain.Entities.ReportDefinition;

namespace WinCCDashboard.Application.Reports;

/// <summary>
/// Turns a deployed report's SSRS parameters into filter controls for the
/// website, and turns the values the user picked back into validated SSRS
/// parameters. One generic mechanism for every report: the controls come
/// from the report itself, plus a few naming conventions used by the
/// WinCC alarm reports (date range, server, priority, category ...).
/// </summary>
public sealed class ReportParameterService
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string SsrsDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
    private const int DefaultRangeDays = 30; // same window the stored procedures use when no dates are passed
    private const int MaxTextLength = 500;   // matches NVARCHAR(500) @Search in the stored procedures

    private static readonly string[] FromNames = { "StartDate", "FromDate", "StartTime", "FromTime", "DateFrom" };
    private static readonly string[] ToNames = { "EndDate", "ToDate", "EndTime", "ToTime", "DateTo" };

    /// <summary>
    /// Technical parameters of the report stored procedures. They are set by
    /// the server and never shown to users. Export=1 makes the procedures
    /// return the complete result instead of the first 500-row page.
    /// </summary>
    private static readonly string[] SystemParameters = { "Export", "PageNumber", "PageSize" };

    /// <summary>
    /// Report-specific fixed values (kept from the previous controller):
    /// these report keys share one RDL and only differ by Category.
    /// Applied only when the deployed report really has the parameter.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, string>> FixedParameters =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["RedundancyEvents"] = new(StringComparer.OrdinalIgnoreCase) { ["Category"] = "Redundancy" },
            ["SystemEvents"] = new(StringComparer.OrdinalIgnoreCase) { ["Category"] = "System" }
        };

    /// <summary>Categories understood by dbo.usp_ReportCategoryEvents.</summary>
    private static readonly ReportFilterOption[] CategoryOptions =
    {
        new("Redundancy", "Redundancy / failover"),
        new("System", "System / connection")
    };

    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["StartDate"] = "From date",
        ["EndDate"] = "To date",
        ["SourceServer"] = "Server",
        ["Priority"] = "Priority",
        ["Search"] = "Alarm name contains",
        ["TopN"] = "Number of alarms",
        ["Category"] = "Event category"
    };

    private readonly ISsrsReportClient _ssrs;
    private readonly IReportRepository _repository;
    private readonly ReportMetadataCache _cache;
    private readonly TimeSpan _cacheTtl;

    public ReportParameterService(
        ISsrsReportClient ssrs,
        IReportRepository repository,
        ReportMetadataCache cache,
        SsrsSettings settings)
    {
        _ssrs = ssrs;
        _repository = repository;
        _cache = cache;
        _cacheTtl = TimeSpan.FromMinutes(Math.Max(0, settings.MetadataCacheMinutes));
    }

    // ------------------------------------------------------------------
    // Filter definition for the UI
    // ------------------------------------------------------------------

    public async Task<ReportFilterDefinition> GetFilterDefinitionAsync(
        ReportEntity report,
        CancellationToken cancellationToken = default)
    {
        var parameters = await GetServerParametersAsync(report, cancellationToken);
        var lookups = await GetLookupsSafeAsync(cancellationToken);
        var fixedValues = GetFixedValues(report, parameters);

        var definition = new ReportFilterDefinition
        {
            ReportKey = report.ReportKey,
            Title = report.Title
        };

        // Date range first (the filter everyone uses), then the report's other
        // parameters in the order the report defines them.
        var userFacing = parameters
            .Where(p => IsUserFacing(p, fixedValues))
            .OrderBy(p => RoleFor(p.Name) == null ? 1 : 0);

        foreach (var p in userFacing)
            definition.Fields.Add(BuildField(p, lookups));

        if (definition.Fields.Any(f => f.Role != null))
        {
            definition.Notes.Add(
                "Dates and times are in UTC, as stored by the alarm logger. " +
                "The To date is inclusive (the whole day is reported).");
        }

        foreach (var (name, value) in fixedValues)
            definition.Notes.Add($"{LabelFor(name, null)}: {value} (fixed for this report).");

        return definition;
    }

    private static ReportFilterField BuildField(ReportServerParameter p, ReportFilterLookups? lookups)
    {
        var role = RoleFor(p.Name);
        var required = IsRequired(p);
        var label = LabelFor(p.Name, p.Prompt);
        var defaults = StaticDefaults(p);

        // Date range (String or DateTime typed) -> date pickers with sensible defaults.
        if (role != null)
        {
            var today = DateTime.UtcNow.Date;
            var fallback = role == "from" ? today.AddDays(-DefaultRangeDays) : today;
            var value = defaults.Count > 0 && TryParseDate(defaults[0], out var d) ? d : fallback;

            return new ReportFilterField
            {
                Name = p.Name,
                Label = label,
                Control = "date",
                Role = role,
                Required = required,
                DefaultValue = value.ToString(DateFormat, CultureInfo.InvariantCulture),
                Help = required ? null : role == "from"
                    ? $"Empty = last {DefaultRangeDays} days"
                    : "Empty = up to now"
            };
        }

        // Lists published by the report itself.
        if (p.ValidValues.Count > 0)
        {
            return new ReportFilterField
            {
                Name = p.Name,
                Label = label,
                Control = p.MultiValue ? "multiselect" : "select",
                Required = required,
                DefaultValue = defaults.FirstOrDefault(),
                DefaultValues = defaults.Where(v => v != null).Select(v => v!).ToList(),
                EmptyOptionLabel = required || p.MultiValue ? null : "All",
                Options = p.ValidValues.Select(v => new ReportFilterOption(v.Value, v.Label)).ToList()
            };
        }

        if (Is(p.Name, "SourceServer") && lookups?.SourceServers.Count > 0)
        {
            return SelectField(p, label, required, defaults, "All servers",
                lookups.SourceServers.Select(s => new ReportFilterOption(s, s)));
        }

        if (Is(p.Name, "Priority") && lookups?.Priorities.Count > 0)
        {
            return SelectField(p, label, required, defaults, "All priorities",
                lookups.Priorities.Select(x => new ReportFilterOption(
                    x.ToString(CultureInfo.InvariantCulture),
                    "Priority " + x.ToString(CultureInfo.InvariantCulture))));
        }

        if (Is(p.Name, "Category") && p.DataType == "String")
            return SelectField(p, label, required, defaults, "All categories", CategoryOptions);

        switch (p.DataType)
        {
            case "Boolean":
                return new ReportFilterField
                {
                    Name = p.Name,
                    Label = label,
                    Control = "checkbox",
                    DefaultValue = defaults.FirstOrDefault() ?? "false"
                };

            case "Integer":
            case "Float":
                var isTopN = Is(p.Name, "TopN");
                return new ReportFilterField
                {
                    Name = p.Name,
                    Label = label,
                    Control = "number",
                    Required = required,
                    DefaultValue = defaults.FirstOrDefault() ?? (isTopN ? "20" : null),
                    Min = isTopN ? 1 : null,
                    Max = isTopN ? 100 : null,
                    Step = p.DataType == "Integer" ? 1 : null,
                    Help = isTopN ? "1 – 100 most frequent alarms" : null
                };

            case "DateTime":
                return new ReportFilterField
                {
                    Name = p.Name,
                    Label = label,
                    Control = "datetime",
                    Required = required,
                    DefaultValue = defaults.FirstOrDefault()
                };

            default:
                return new ReportFilterField
                {
                    Name = p.Name,
                    Label = label,
                    Control = "text",
                    Required = required,
                    DefaultValue = defaults.FirstOrDefault(),
                    MaxLength = MaxTextLength,
                    Placeholder = Is(p.Name, "Search") ? "e.g. Redundancy, PLC_1 …" : null
                };
        }
    }

    private static ReportFilterField SelectField(
        ReportServerParameter p,
        string label,
        bool required,
        List<string?> defaults,
        string emptyLabel,
        IEnumerable<ReportFilterOption> options)
    {
        return new ReportFilterField
        {
            Name = p.Name,
            Label = label,
            Control = "select",
            Required = required,
            DefaultValue = defaults.FirstOrDefault(),
            EmptyOptionLabel = required ? null : emptyLabel,
            Options = options.ToList()
        };
    }

    // ------------------------------------------------------------------
    // User values -> SSRS parameters
    // ------------------------------------------------------------------

    /// <summary>
    /// Validates the submitted filter values against the report's real
    /// parameters and converts them to the culture-invariant format SSRS
    /// and the stored procedures expect.
    /// </summary>
    /// <param name="values">Submitted values by parameter name.</param>
    /// <param name="rejectUnknown">When true, a value for a parameter the report does not have is an error.</param>
    public async Task<ReportParameterBuildResult> BuildParametersAsync(
        ReportEntity report,
        IReadOnlyDictionary<string, string?[]> values,
        bool rejectUnknown = true,
        CancellationToken cancellationToken = default)
    {
        var parameters = await GetServerParametersAsync(report, cancellationToken);
        var fixedValues = GetFixedValues(report, parameters);
        var result = new ReportParameterBuildResult();

        if (rejectUnknown)
        {
            foreach (var name in values.Keys)
            {
                if (!parameters.Any(p => Is(p.Name, name)))
                    result.AddError(string.Empty, $"'{name}' is not a filter of this report.");
            }
        }

        DateTime? from = null, toExclusive = null;
        string? fromName = null, toName = null;

        foreach (var p in parameters)
        {
            if (fixedValues.TryGetValue(p.Name, out var fixedValue))
            {
                result.Parameters.Add(new(p.Name, fixedValue));
                continue;
            }

            if (!p.PromptUser)
                continue; // internal parameter: SSRS computes it

            if (IsSystemParameter(p.Name))
            {
                AddSystemParameter(p, result);
                continue;
            }

            var label = LabelFor(p.Name, p.Prompt);
            var submitted = values
                .Where(kv => Is(kv.Key, p.Name))
                .SelectMany(kv => kv.Value)
                .Select(v => v?.Trim())
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v!)
                .ToList();

            if (submitted.Count == 0)
            {
                AddEmptyValue(p, label, result);
                continue;
            }

            if (submitted.Count > 1 && !p.MultiValue)
            {
                result.AddError(p.Name, $"{label} accepts a single value.");
                continue;
            }

            var role = RoleFor(p.Name);

            foreach (var raw in submitted)
            {
                if (role != null)
                {
                    if (!TryParseDateOrDateTime(raw, out var date, out var hasTime))
                    {
                        result.AddError(p.Name, $"{label} is not a valid date.");
                        continue;
                    }

                    if (role == "from")
                    {
                        from = date;
                        fromName = p.Name;
                    }
                    else
                    {
                        // The procedures filter RaiseTime < @EndDate. A plain date
                        // means "through the end of that day", so send the next midnight.
                        date = hasTime ? date : date.AddDays(1);
                        toExclusive = date;
                        toName = p.Name;
                    }

                    result.Parameters.Add(new(p.Name, date.ToString(SsrsDateTimeFormat, CultureInfo.InvariantCulture)));
                    continue;
                }

                if (!TryConvert(p, raw, out var converted, out var error))
                {
                    result.AddError(p.Name, string.Format(CultureInfo.InvariantCulture, error, label));
                    continue;
                }

                result.Parameters.Add(new(p.Name, converted));
            }
        }

        if (from.HasValue && toExclusive.HasValue && from.Value >= toExclusive.Value)
        {
            result.AddError(toName ?? string.Empty,
                $"{LabelFor(fromName!, null)} must be on or before {LabelFor(toName!, null)}.");
        }

        return result;
    }

    private static void AddSystemParameter(ReportServerParameter p, ReportParameterBuildResult result)
    {
        if (Is(p.Name, "Export"))
        {
            result.Parameters.Add(new(p.Name, p.DataType == "Boolean" ? "true" : "1"));
            return;
        }

        // PageNumber / PageSize are ignored by the procedures in export mode.
        if (p.Nullable)
            result.Parameters.Add(new(p.Name, null));
        else if (!p.HasDefault)
            result.Parameters.Add(new(p.Name, Is(p.Name, "PageNumber") ? "1" : "500"));
    }

    private static void AddEmptyValue(ReportServerParameter p, string label, ReportParameterBuildResult result)
    {
        if (p.Nullable)
        {
            // Explicit NULL = "no filter"; the stored procedures apply their own defaults.
            result.Parameters.Add(new(p.Name, null));
        }
        else if (p.DataType == "String" && p.AllowBlank)
        {
            result.Parameters.Add(new(p.Name, string.Empty));
        }
        else if (p.DataType == "Boolean")
        {
            result.Parameters.Add(new(p.Name, "false"));
        }
        else if (StaticDefaults(p).Any(v => v != null))
        {
            // Let SSRS apply the report's own default value.
        }
        else
        {
            result.AddError(p.Name, $"{label} is required.");
        }
    }

    private static bool TryConvert(ReportServerParameter p, string raw, out string? converted, out string error)
    {
        converted = null;
        error = string.Empty;

        switch (p.DataType)
        {
            case "Integer":
                if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)
                    || l < int.MinValue || l > int.MaxValue)
                {
                    error = "{0} must be a whole number.";
                    return false;
                }
                if (Is(p.Name, "TopN") && (l < 1 || l > 100))
                {
                    error = "{0} must be between 1 and 100.";
                    return false;
                }
                converted = l.ToString(CultureInfo.InvariantCulture);
                break;

            case "Float":
                if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var dec))
                {
                    error = "{0} must be a number.";
                    return false;
                }
                converted = dec.ToString(CultureInfo.InvariantCulture);
                break;

            case "Boolean":
                if (raw.Equals("on", StringComparison.OrdinalIgnoreCase) || raw == "1")
                    raw = "true";
                else if (raw == "0")
                    raw = "false";

                if (!bool.TryParse(raw, out var b))
                {
                    error = "{0} must be true or false.";
                    return false;
                }
                converted = b ? "true" : "false";
                break;

            case "DateTime":
                if (!TryParseDateOrDateTime(raw, out var dt, out _))
                {
                    error = "{0} is not a valid date/time.";
                    return false;
                }
                converted = dt.ToString(SsrsDateTimeFormat, CultureInfo.InvariantCulture);
                break;

            default:
                if (raw.Length > MaxTextLength)
                {
                    error = "{0} is too long (maximum " + MaxTextLength + " characters).";
                    return false;
                }
                converted = raw;
                break;
        }

        // Static valid-values lists published by the report are enforced here,
        // and so are the fixed category names the procedure understands.
        var candidate = converted;

        if (p.ValidValues.Count > 0 && !p.ValidValuesQueryBased
            && !p.ValidValues.Any(v => string.Equals(v.Value, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            error = "{0} has a value that is not allowed.";
            return false;
        }

        if (Is(p.Name, "Category") && p.DataType == "String"
            && !CategoryOptions.Any(o => string.Equals(o.Value, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            error = "{0} has a value that is not allowed.";
            return false;
        }

        return true;
    }

    // ------------------------------------------------------------------
    // Shared helpers
    // ------------------------------------------------------------------

    private Task<IReadOnlyList<ReportServerParameter>> GetServerParametersAsync(
        ReportEntity report,
        CancellationToken cancellationToken)
    {
        return _cache.GetOrAddAsync(
            "ssrs-params:" + report.RdlName,
            _cacheTtl,
            ct => _ssrs.GetReportParametersAsync(report.RdlName, ct),
            cancellationToken);
    }

    private async Task<ReportFilterLookups?> GetLookupsSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _cache.GetOrAddAsync(
                "report-lookups",
                _cacheTtl,
                ct => _repository.GetFilterLookupsAsync(ct),
                cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Lookups only improve the UI (dropdowns instead of free text);
            // the report must still be usable when they cannot be loaded.
            return null;
        }
    }

    private static Dictionary<string, string> GetFixedValues(
        ReportEntity report,
        IReadOnlyList<ReportServerParameter> parameters)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (FixedParameters.TryGetValue(report.ReportKey, out var fixedForReport))
        {
            foreach (var (name, value) in fixedForReport)
            {
                var p = parameters.FirstOrDefault(x => Is(x.Name, name));
                if (p != null)
                    result[p.Name] = value;
            }
        }

        return result;
    }

    private static bool IsUserFacing(ReportServerParameter p, Dictionary<string, string> fixedValues) =>
        p.PromptUser && !fixedValues.ContainsKey(p.Name) && !IsSystemParameter(p.Name);

    private static bool IsSystemParameter(string name) =>
        SystemParameters.Any(s => Is(s, name));

    private static bool IsRequired(ReportServerParameter p) =>
        !p.Nullable && !(p.DataType == "String" && p.AllowBlank) && p.DataType != "Boolean";

    private static string? RoleFor(string name) =>
        FromNames.Any(n => Is(n, name)) ? "from" :
        ToNames.Any(n => Is(n, name)) ? "to" : null;

    private static string LabelFor(string name, string? prompt) =>
        Labels.TryGetValue(name, out var label) ? label
        : !string.IsNullOrWhiteSpace(prompt) ? prompt!
        : name;

    private static List<string?> StaticDefaults(ReportServerParameter p) =>
        p.DefaultValuesQueryBased ? new List<string?>() : p.DefaultValues.Where(v => v != null).ToList();

    private static bool Is(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    /// <summary>Parses a report default value (SSRS may return DateTime defaults in en-US form).</summary>
    private static bool TryParseDate(string? value, out DateTime date) =>
        TryParseDateOrDateTime(value, out date, out _, allowServerFormats: true);

    /// <summary>ISO 8601 only for user input: unambiguous in every culture (no dd/MM vs MM/dd guessing).</summary>
    private static readonly string[] IsoFormats =
    {
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd HH:mm:ss"
    };

    private static readonly string[] ServerDefaultFormats = IsoFormats
        .Concat(new[] { "M/d/yyyy h:mm:ss tt", "M/d/yyyy" })
        .ToArray();

    private static bool TryParseDateOrDateTime(
        string? value, out DateTime date, out bool hasTime, bool allowServerFormats = false)
    {
        hasTime = false;
        date = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!DateTime.TryParseExact(value.Trim(), allowServerFormats ? ServerDefaultFormats : IsoFormats,
                CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date))
        {
            return false;
        }

        hasTime = date.TimeOfDay != TimeSpan.Zero;
        return date.Year is >= 1753 and <= 9999;
    }
}
