using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace GameRoomsService
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var webHost = BuildWebHost(args);

                webHost.Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Application startup failed: " + ex.Message);
                return -1;
            }

            return 0;
        }

        private static IWebHost BuildWebHost(string[] args)
        {
            var webHost = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
            return webHost;
        }
    }
}

