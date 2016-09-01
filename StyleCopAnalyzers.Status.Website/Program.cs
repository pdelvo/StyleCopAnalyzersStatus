﻿using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace StyleCopAnalyzers.Status.Website
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseUrls("http://0.0.0.0:5000/")
                .Build();

            host.Run();
        }
    }
}
