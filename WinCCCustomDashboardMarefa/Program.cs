using PdfSharp.Fonts;
using System.Text;
using WinCCCustomDashboardMarefa.Pdf;
using WinCCCustomDashboardMarefa.Services;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
GlobalFontSettings.UseWindowsFontsUnderWindows = true;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<AlarmService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ReportsService>();
builder.Services.AddScoped<AlarmHistoryPdfService>();
builder.Services.AddScoped<DailyAlarmSummaryPdfService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();