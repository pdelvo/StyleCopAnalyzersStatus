using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StyleCopAnalyzers.Status.Website.Models;

namespace StyleCopAnalyzers.Status.Website
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Setup configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container.
            services.AddMvc();
            
            services.Configure<StatusPageOptions>(options => Configuration.Bind(options));
            services.AddMemoryCache();

            var value = ConfigurationBinder.GetValue<DataProvider>(Configuration, "DataProvider");
            switch (value)
            {
                case DataProvider.File:
                    services.AddSingleton<IDataResolver, FileDataResolver>();
                    break;
                case DataProvider.Web:
                    services.AddSingleton<IDataResolver, WebDataResolver>();
                    break;
                default:
                    throw new Exception("Data provider not supported");
            }
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            // Configure the HTTP request pipeline.

            // Add the following to the request pipeline only in development environment.
            app.UseDeveloperExceptionPage();
            if (env.IsDevelopment())
            {
            }
            else
            {
                // Add Error handling middleware which catches all application specific errors and
                // send the request to the following path or controller action.
                app.UseExceptionHandler("/Home/Error");
            }

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Add MVC to the request pipeline.
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                // Uncomment the following line to add a route for porting Web API 2 controllers.
                // routes.MapWebApiRoute("DefaultApi", "api/{controller}/{id?}");
            });
        }
    }
}
