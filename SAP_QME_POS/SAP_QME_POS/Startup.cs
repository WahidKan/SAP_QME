using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SAP_QME_POS.Connection;
using SAP_QME_POS.Model.Setting;
using SAP_QME_POS.Service;
using SAP_QME_POS.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAP_QME_POS
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
            services.Configure<Setting>(Configuration.GetSection("SAP"));
            services.AddSingleton<ISAP_Connection>(x => new SAP_Connection(x.GetService<Setting>()));

            services.AddSingleton<DIService>();
            services.AddHostedService(sp => sp.GetRequiredService<DIService>());
            services.AddSingleton<IARInvoiceExtension, ARInvoiceExtension>();
            services.AddSingleton<IDataContext>(x => new DataContext(x.GetService<Setting>()));
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
