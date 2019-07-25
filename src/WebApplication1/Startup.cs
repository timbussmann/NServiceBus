using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;

namespace WebApplication1
{
    using System;
    using Endpoint = NServiceBus.Endpoint;

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

            // register handlers and stuff even before configuring the endpoint
            services.AddNServiceBus();

            //register some service required by a message handler
            services.AddSingleton<SomeDependency>();

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
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc();

            //TODO this could be wrapped in app.UseNServiceBus(...);
            var endpointConfiguration = new EndpointConfiguration("aspnet");
            endpointConfiguration.UseTransport<LearningTransport>();
            endpointConfiguration.UsePersistence<LearningPersistence>();

            endpointConfiguration.RegisterResolver(ctx =>
            {
                var scope = app.ApplicationServices.CreateScope();
                ctx.Set((Func<Type, object>)scope.ServiceProvider.GetService);
                return scope;
            });

            //TODO how to register IMessageSession with this?
            var endpoint = Endpoint.Start(endpointConfiguration).GetAwaiter().GetResult();

            endpoint.SendLocal(new TestMessage()).GetAwaiter().GetResult();
        }
    }
}
