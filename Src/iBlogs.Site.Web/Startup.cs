﻿using iBlogs.Site.Web.Converter;
using iBlogs.Site.Web.Filter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using iBlogs.Site.Core.Startup;
using iBlogs.Site.Core.Startup.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;

namespace iBlogs.Site.Web
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIBlogs();

            services.AddControllersWithViews(option =>
            {
                option.Filters.Add<LoginFilter>();
                option.Filters.Add<ExceptionFilter>();
            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new BlogsContractResolver();
            });
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStatusCodePages(async context =>
            {
                var response = context.HttpContext.Response;
                if (response.StatusCode == (int)HttpStatusCode.Unauthorized)
                {
                   response.Redirect("/admin/login");
                }
                else if (!env.IsDevelopment())
                {
                    if (response.StatusCode == (int)HttpStatusCode.NotFound)
                    {
                        response.Redirect("/error/error404");
                    }
                    else if (response.StatusCode == (int)HttpStatusCode.InternalServerError)
                    {
                        response.Redirect("/error/error500");
                    }
                    else
                    {
                        response.Redirect("/Error/Index/" + response.StatusCode);
                    }
                }
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMiddleware<DataSyncCheckMiddleware>();

            app.UseEndpoints(routes =>
            {
                routes.MapControllerRoute(
                    name: "Admin",
                    "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                routes.MapDefaultControllerRoute();
            });
        }
    }
}