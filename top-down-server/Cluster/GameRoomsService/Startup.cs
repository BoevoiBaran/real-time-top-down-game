using GameRoomsService.GameRoomServer;
using GameRoomsService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GameRoomsService
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<GameRoomsServerSettings>(Configuration.GetSection(GameRoomsServerSettings.SECTION));
            
            services.AddControllers();
            services.AddHealthChecks();

            services.AddSingleton<RoomsHolder>();
            services.AddSingleton<GameRoomsServer>();
            services.AddHostedService<GameRoomsManager>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", async context =>
                {
                    context.Response.ContentType = "text/html";
                    const string result = @"<DOCTYPE html>" +
                                          @"<html><body>" +
                                          @"<il>" +
                                          @"<li><a href=""/ServiceStatus"">Service Status</a></li>" +
                                          @"<li><a href=""/health"">Health</a></li>" +
                                          @"</il>" +
                                          @"</body></html>";
                    await context.Response.WriteAsync(result);
                });
                
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}