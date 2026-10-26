using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
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
        services.AddScoped<IReportDefinitionRepository, ReportDefinitionRepository>();

        // PDF
        services.AddSingleton<IAlarmHistoryPdfGenerator, AlarmHistoryPdfGenerator>();
        services.AddSingleton<IDailySummaryPdfGenerator, DailySummaryPdfGenerator>();

        // SSRS Reporting (server-side only; the browser never calls SSRS)
        services.Configure<SsrsSettings>(configuration.GetSection("SsrsSettings"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<SsrsSettings>>().Value);

        services.AddHttpClient<ISsrsReportClient, SsrsReportClient>()
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<SsrsSettings>>().Value;

                var handler = new HttpClientHandler
                {
                    PreAuthenticate = true
                };

                if (!string.IsNullOrWhiteSpace(settings.Username))
                {
                    // Dedicated service account (password from user-secrets / environment).
                    handler.Credentials = new NetworkCredential(
                        settings.Username, settings.Password, settings.Domain);
                }
                else
                {
                    // Identity of the web app process (IIS app pool / Windows service account).
                    handler.UseDefaultCredentials = true;
                }

                return handler;
            });

        return services;
    }
}
