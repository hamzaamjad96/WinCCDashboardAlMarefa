# Reporting module (SSRS)

## How it works

```
Browser ──► /Reports/Show?key=…            viewer page (selector, filters, viewer)
        ──► /Reports/Parameters?key=…      filter definition (JSON) built from the deployed report
        ──► /Reports/Render?key=…&format=PDF&StartDate=…   PDF/Excel/Word/CSV (same origin)
                    │
                    ▼   server side only, Windows credentials of the web app
              SSRS  /ReportServer/ReportService2010.asmx  (GetItemParameters)
                    /ReportServer?/WinCCDashboard/<RdlName>&rs:Format=PDF&…
```

The browser never sees an SSRS URL or credential. The PDF is displayed in the
browser's own PDF viewer inside the page (page navigation, zoom, print,
download). Excel, Word and CSV downloads use the same endpoint.

Filters are not hard-coded per report. The site reads each report's real
parameters from SSRS and draws matching controls:

| Report parameter | Control | Sent to SSRS |
|---|---|---|
| `StartDate` / `EndDate` (String or DateTime) | date pickers, default last 30 days, quick ranges | `yyyy-MM-ddT00:00:00`; **To date is inclusive** → next midnight (procedures use `RaiseTime < @EndDate`) |
| `Priority` | dropdown (distinct values from `AlarmEvents`) | integer or NULL (all) |
| `SourceServer` | dropdown (distinct values from `AlarmEvents`) | text or NULL (all) |
| `Search` | text, max 500 | text or NULL |
| `TopN` | number 1–100, default 20 | integer |
| `Category` | dropdown Redundancy / System (fixed for report keys `RedundancyEvents`, `SystemEvents`) | text |
| `Export`, `PageNumber`, `PageSize` | hidden | `Export=true` (full result, not the first 500-row page), paging NULL |
| anything else | by type: number, checkbox, text, date/time, or the report's own valid-values list | validated by type |

Empty optional filters are sent as explicit NULL, so the stored procedures'
own defaults apply (e.g. last 30 days). All values are validated on the
server; parameters the report does not have and `rs:`/`rc:` commands are
rejected.

## Configuration (`appsettings.json` → `SsrsSettings`)

| Key | Meaning |
|---|---|
| `BaseUrl` | Report server **web service** URL, e.g. `http://REPORTSERVER/ReportServer` (not the `/Reports` portal). Reached by the web server only. |
| `ReportsFolder` | Folder holding the reports, e.g. `/WinCCDashboard` |
| `TimeoutMinutes` | Render timeout (default 5) |
| `MetadataCacheMinutes` | Cache for report parameters and dropdown lists (default 10) |
| `Username`, `Domain` | Optional service account for SSRS. Empty = identity of the web app process |
| `Password` | Never in appsettings. Use `dotnet user-secrets` or the environment variable `SsrsSettings__Password` |

## Deployment checklist

1. Upload the RDL files from `reports/` to the SSRS folder `/WinCCDashboard`
   (web portal → Upload, overwrite). They use the shared data source
   `/WinCCReportingDS`.
2. Make sure `dbo.ReportDefinitions.RdlName` matches the deployed report names
   (`database/Reporting_Recommended.sql` lists them).
3. Give the web application's identity the **Browser** role on the
   `/WinCCDashboard` folder in SSRS.
   * IIS: the app pool identity. When SSRS is on another machine the app
     pool's network identity is the computer account (`DOMAIN\WEBSERVER$`)
     unless a domain service account is configured.
   * Or set `SsrsSettings:Username/Domain` + `SsrsSettings__Password`.
4. Set `SsrsSettings:BaseUrl` to the URL the **web server** can reach.
5. Optional: run `database/Reporting_Recommended.sql` (index on `RaiseTime`).

## Troubleshooting (messages shown in the viewer)

| Message | Cause |
|---|---|
| "…is not deployed on the report server…" | RdlName / folder mismatch (`rsItemNotFound`) |
| "The web application is not allowed to run this report…" | 401/403 from SSRS: grant Browser role (step 3) |
| "…could not read the report data…" | Shared data source credentials / SQL permissions |
| "The report server could not be reached…" | `BaseUrl` wrong or SSRS service stopped |
| "…took too long…" | Increase `TimeoutMinutes`, or narrow the date range |

Full SSRS error details are written to the application log, never to the browser.

## Time zone

Alarm times are stored in UTC (WinCC GraphQL `raiseTime`). Filters, report
columns and the header are labelled UTC.
