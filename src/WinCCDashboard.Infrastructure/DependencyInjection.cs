using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WinCCDashboard.Application.Abstractions.Pdf;
using WinCCDashboard.Application.Abstractions.Persistence;
using WinCCDashboard.Infrastructure.Pdf;
using WinCCDashboard.Infrastructure.Persistence;
using WinCCDashboard.Infrastructure.Persistence.Repositories;

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

        // PDF
        services.AddSingleton<IAlarmHistoryPdfGenerator, AlarmHistoryPdfGenerator>();
        services.AddSingleton<IDailySummaryPdfGenerator, DailySummaryPdfGenerator>();

        return services;
    }
}