using BTCPayServerDockerConfigurator;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "CONFIGURATOR_");

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation()
    .AddSessionStateTempDataProvider();
builder.Services.AddConfigurator();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

var options = app.Services.GetRequiredService<IOptions<ConfiguratorOptions>>();
if (!string.IsNullOrEmpty(options.Value.RootPath))
{
    app.UsePathBase(options.Value.RootPath);
}
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{action=Index}/{id?}");

app.Run();
