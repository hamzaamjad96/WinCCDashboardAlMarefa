using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WinCCDashboard.Application.Abstractions.Pdf;
using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Application.Abstractions.Reporting;
using WinCCDashboard.Domain.Entities;
using WinCCDashboard.Infrastructure.Pdf;
using WinCCDashboard.Infrastructure.Persistence;
using WinCCDashboard.Infrastructure.Persistence.Repositories;
using WinCCDashboard.Infrastructure.Reporting;

namespace WinCCDashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Persistence
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IAlarmRepository, AlarmRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportDefinitionRepository, ReportDefinitionRepository>();  // ← YE ADD

        // PDF
        services.AddSingleton<IAlarmHistoryPdfGenerator, AlarmHistoryPdfGenerator>();
        services.AddSingleton<IDailySummaryPdfGenerator, DailySummaryPdfGenerator>();

        // SSRS Reporting
        services.Configure<SsrsSettings>(configuration.GetSection("SsrsSettings"));

        services.AddHttpClient<ISsrsReportClient, SsrsReportClient>()
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    UseDefaultCredentials = true,
                    PreAuthenticate = true
                };
            });

        return services;
    }
}