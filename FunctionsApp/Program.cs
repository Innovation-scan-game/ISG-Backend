﻿using System.IO;
using Microsoft.Extensions.Hosting;

namespace FunctionsApp;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new HostBuilder()
            // .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            // .UseIISIntegration()
            // .UseStartup<Startup>()
            .Build();

        host.Run();
    }
}
