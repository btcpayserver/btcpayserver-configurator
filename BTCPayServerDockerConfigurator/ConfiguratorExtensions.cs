using System;
using System.IO;
using BTCPayServerDockerConfigurator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServerDockerConfigurator
{
    public static class ConfiguratorExtensions
    {
        public static IMvcBuilder AddConfigurator(this IMvcBuilder mvcBuilder, IServiceCollection services, IConfiguration Configuration)
        {
            services.AddOptions();
            services.Configure<ConfiguratorOptions>(Configuration);
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