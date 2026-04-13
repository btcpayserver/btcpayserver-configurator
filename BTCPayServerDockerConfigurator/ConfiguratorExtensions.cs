using BTCPayServerDockerConfigurator.Models;

namespace BTCPayServerDockerConfigurator;

public static class ConfiguratorExtensions
{
    public static IServiceCollection AddConfigurator(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddSingleton<DeploymentService>();
        services.AddOptions<ConfiguratorOptions>();
        services.AddHttpClient();
        services.PostConfigure<ConfiguratorOptions>(options =>
        {
            if (!string.IsNullOrEmpty(options.CookieFilePath))
            {
                File.WriteAllText(options.CookieFilePath, Guid.NewGuid().ToString());
            }
        });
        services.AddSession();
        return services;
    }
}
