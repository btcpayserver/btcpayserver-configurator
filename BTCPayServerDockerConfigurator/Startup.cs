using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Options = BTCPayServerDockerConfigurator.Models.Options;

namespace BTCPayServerDockerConfigurator
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<Options>(Configuration);
            services.PostConfigure<Options>(async options =>
            {
                if (!string.IsNullOrEmpty(options.PasswordFilePath))
                {
                    await File.WriteAllTextAsync(options.PasswordFilePath, Guid.NewGuid().ToString());
                }
            });
            services.AddSingleton<DeploymentService>();
            services.AddControllersWithViews()
                .AddSessionStateTempDataProvider()
                .AddRazorRuntimeCompilation();
            services.AddSession();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger, IOptions<Options> options)
        {
            
            logger.LogInformation(JsonSerializer.Serialize(options));
            ConfigureCore(app, env, options);
        }

        private static void ConfigureCore(IApplicationBuilder app, IWebHostEnvironment env, IOptions<Options> options)
        {
            app.UseDeveloperExceptionPage();
            if (env.IsDevelopment())
            {
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseForwardedHeaders();
            app.UseHttpsRedirection();
            app.UseStaticFiles(options.Value.RootPath);
            app.UsePathBase(options.Value.RootPath);
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}