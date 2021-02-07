using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Msg.Core.UniPush;
using WeiCloudStorageAPI.Data;
using WeiCloudStorageAPI.Hubs;
using WeiCloudStorageAPI.Model;
using WeiCloudStorageAPI.Services;
using WeiCloudStorageAPI.Util;

namespace WeiCloudStorageAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        private string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var isconfig = Configuration.GetSection("IdentityClientConfig").Get<IdentityClientConfig>();
            //services.AddAuthentication(isconfig.Scheme)
            //    .AddIdentityServerAuthentication(options =>
            //    {
            //        options.RequireHttpsMetadata = isconfig.RequireHttpsMetadata;
            //        options.Authority = isconfig.Authority;
            //        options.ApiName = isconfig.ApiName;
            //    }
            //    );

            services.AddAuthentication(isconfig.Scheme)
                .AddJwtBearer(isconfig.Scheme, options =>
                {
                    options.RequireHttpsMetadata = isconfig.RequireHttpsMetadata;
                    options.Authority = isconfig.Authority;
                    //options.Audience = isconfig.ApiName;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false
                    };
                });

            services.AddCors(options =>
                 options.AddPolicy(MyAllowSpecificOrigins,
                 p => p.WithOrigins("http://*.*.*.*")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                        .SetIsOriginAllowed(s => true))
            );
            services.AddSingleton(typeof(UniPushUtil));
            services.AddSingleton<IPcMsgService, PcMsgService>();
            services.AddSingleton<IUniAppMsgService, UniAppMsgService>();
            services.AddSingleton<IAppPackageService, AppPackageService>();
            services.AddSingleton(typeof(RedisHelper));
            services.AddSingleton(typeof(StringCache));
            services.AddSingleton(typeof(HashCache));
            services.AddSingleton(typeof(MsgCache));
            services.AddSingleton(typeof(DBContext));
            services.AddSingleton(typeof(DBContextAir));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IQRCode, QRCode>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseAuthentication();
            app.UseCors(MyAllowSpecificOrigins);
            
            app.UseSignalR(endpoints =>
            {
                endpoints.MapHub<VideoHub>("/videoHub");
                endpoints.MapHub<MsgHub>("/msgHub");
            });
            app.UseMvc();
        }
    }
}
