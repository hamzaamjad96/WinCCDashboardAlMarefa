/* =====================================================================
   Reporting – recommended, optional database changes
   ---------------------------------------------------------------------
   The website fix does NOT depend on this script. No table, column or
   stored procedure is changed. Review and run on the reporting database
   when convenient.
   ===================================================================== */

USE [WinCCUnifiedDashboardDBSabil];
GO

/* 1. Performance ------------------------------------------------------
   Every report procedure filters on RaiseTime ranges
   (RaiseTime >= @StartDate AND RaiseTime < @EndDate) but AlarmEvents only
   has its clustered primary key on Id, so each report scans the whole
   table. This index makes date-range reports and the worker's duplicate
   check (usp_InsertAlarmEvent) seek instead of scan. */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_AlarmEvents_RaiseTime'
      AND object_id = OBJECT_ID(N'dbo.AlarmEvents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AlarmEvents_RaiseTime
        ON dbo.AlarmEvents (RaiseTime)
        INCLUDE (AlarmName, Priority, SourceServer, SyncedAt);
END
GO

/* 2. Permissions (only if needed) --------------------------------------
   The reports call stored procedures. If the SSRS shared data source
   /WinCCReportingDS signs in as ssrs_user, note that db_datareader (the
   only role granted in the schema script) does not include EXECUTE.
   Uncomment if reports fail with "EXECUTE permission was denied". */
-- GRANT EXECUTE ON SCHEMA::dbo TO [ssrs_user];
GO

/* 3. Check the report catalog -----------------------------------------
   ReportDefinitions.RdlName must equal the report name deployed in the
   SSRS folder configured in SsrsSettings:ReportsFolder (/WinCCDashboard).
   Report keys "RedundancyEvents" and "SystemEvents" may both point to
   ReportCategoryEvents: the website then fixes Category automatically. */
SELECT ReportKey, Title, Category, RdlName, IsActive, SortOrder
FROM dbo.ReportDefinitions
ORDER BY Category, SortOrder, Title;
GO
