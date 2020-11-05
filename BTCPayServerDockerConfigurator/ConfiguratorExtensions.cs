using System;
using System.IO;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServerDockerConfigurator
{
    public static class ConfiguratorExtensions
    {
        public static IMvcBuilder AddConfigurator(this IMvcBuilder mvcBuilder, IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<DeploymentService>();
            services.AddOptions<ConfiguratorOptions>();
            services.PostConfigure<ConfiguratorOptions>(async options =>
            {
                if (!string.IsNullOrEmpty(options.CookieFilePath))
                {
                    await File.WriteAllTextAsync(options.CookieFilePath, Guid.NewGuid().ToString());
                }
            });
            services.AddSession();
            return mvcBuilder.AddSessionStateTempDataProvider();
        }
    }
}