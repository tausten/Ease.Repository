using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ease.Repository;
using Ease.Repository.AzureTable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleDataLayer;

namespace SampleCoreWebApp
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            #region Repository pattern registration
            // Make sure we get a new instance of the unit of work per "Scope", but allow that instance to be obtained by 
            // either interface.
            services.AddScoped<IBestEffortUnitOfWork, BestEffortUnitOfWork>();
            services.AddScoped<IUnitOfWork>(x => x.GetRequiredService<IBestEffortUnitOfWork>());

            // The AzureTable storage config provider
            services.AddSingleton<SampleAzureTableMainRepositoryContext.StorageConfig>();

            // The StoreFactory
            services.AddSingleton<IAzureTableStoreFactory, AzureTableStoreFactory>();

            // Make sure we get a new instance of the context per "Scope" with same trick for multi-interface registration as before.
            services.AddScoped<SampleAzureTableMainRepositoryContext>();
            services.AddScoped<IAzureTableRepositoryContext>(x => x.GetRequiredService<SampleAzureTableMainRepositoryContext>());

            // Register our repositories (again, "Scoped")
            services.AddScoped<CustomerAzureTableRepository>();
            services.AddScoped<ProductAzureTableRepository>();
            #endregion // Repository pattern

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
