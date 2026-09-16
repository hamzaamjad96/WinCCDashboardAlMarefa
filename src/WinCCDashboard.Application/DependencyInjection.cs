using Microsoft.Extensions.DependencyInjection;
using WinCCDashboard.Application.Alarms;
using WinCCDashboard.Application.Dashboard;
using WinCCDashboard.Application.Reports;

namespace WinCCDashboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<AlarmQueryService>();
        services.AddScoped<DashboardQueryService>();
        services.AddScoped<ReportQueryService>();
        return services;
    }
}