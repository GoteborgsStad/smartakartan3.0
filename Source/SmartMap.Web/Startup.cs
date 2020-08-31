using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using SmartMap.Web.Infrastructure;
using SmartMap.Web.Routers;
//using Elastic.Apm.AspNetCore;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Rewrite;
using SmartMap.Web.Routers;
using SmartMap.Web.Util;

namespace SmartMap.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var elasticUri = configuration["web-app:elastic-url"];
            var username = configuration["web-app:elastic-username"];
            var password = configuration["web-app:elastic-password"];
            //var elasticsearchIndex = "indexname-{0:yyyy.MM.dd}";

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Debug()
#endif
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
                {
#if !DEBUG
                    ModifyConnectionSettings = x => x.BasicAuthentication(username, password),
#endif
                    AutoRegisterTemplate = true,
                    //IndexFormat = elasticsearchIndex
                })
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
#if DEBUG
            // for ASP.NET Core 3.0 add Runtime Razor Compilation
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddMvc().AddRazorRuntimeCompilation();
#endif

            services.AddLazyCache();

            // Compression
            // url: https://gunnarpeipman.com/aspnet-core-compress-gzip-brotli-content-encoding/
            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });
            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });
            services.AddResponseCompression(options =>
            {
                IEnumerable<string> MimeTypes = new[]
                {
                    "text/plain",
                    "text/html",
                    "text/css",
                    "font/woff2",
                    "application/javascript",
                    "image/x-icon",
                    "image/png"
                };

                options.EnableForHttps = true;
                options.ExcludedMimeTypes = MimeTypes;
                options.Providers.Add<GzipCompressionProvider>();
                options.Providers.Add<BrotliCompressionProvider>();
            });

            // DI
            services.AddSingleton<CmsTransformer>();

            // Infrastructure
            services.AddTransient<ICmsApiProxy, CmsApiProxy>();
            services.AddTransient<IRouteHandler, RouteHandler>();
            services.AddTransient<IBusinessRepository, BusinessRepository>();
            services.AddTransient<ITagRepository, TagRepository>();
            services.AddTransient<IRegionRepository, RegionRepository>();

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                logger.LogInformation("In Development environment");
                app.UseDeveloperExceptionPage();

                app.UseResponseCompression();
                app.UseStaticFiles();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();

                app.UseResponseCompression();
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = content =>
                    {
                        if (content.File.Name.EndsWith(".js.gz"))
                        {
                            content.Context.Response.Headers["Content-Type"] = "application/javascript";
                            content.Context.Response.Headers["Content-Encoding"] = "gzip";
                        }
                        if (content.File.Name.EndsWith(".css.gz"))
                        {
                            content.Context.Response.Headers["Content-Type"] = "text/css";
                            content.Context.Response.Headers["Content-Encoding"] = "gzip";
                        }
                    }
                });
            }

            app.UseRewriter(new RewriteOptions()
                .AddRedirectToWwwPermanent()
            );

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            loggerFactory.AddSerilog();
            // TODO: Disable APM for the moment... many connection errors regarding APM
            //app.UseElasticApm(Configuration);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDynamicControllerRoute<CmsTransformer>("{language?}");
                endpoints.MapDynamicControllerRoute<CmsTransformer>("{language}/{region}");
                endpoints.MapDynamicControllerRoute<CmsTransformer>("{language}/{region}/{page}");
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
