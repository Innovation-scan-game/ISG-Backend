using System;
using DAL.Data;
using FunctionsApp;
using IsolatedFunctions.Infrastructure;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace FunctionsApp;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddHttpClient();

        // use cors

        // builder.Services.AddCors(options =>
        // {
        //     options.AddPolicy(name: "MyPolicy", policy =>
        //     {
        //         policy.WithOrigins("http://127.0.0.1:8000")
        //             .AllowAnyHeader()
        //             .AllowAnyMethod()
        //             .AllowAnyOrigin();
        //     });
        // });



        builder.Services.AddAutoMapper(typeof(InnovationGameMappingProfile));

        // add signalr


        string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        builder.Services.AddDbContext<InnovationGameDbContext>();
        // builder.Services.AddDbContext<InnovationGameDbContext>(
        //     options => options.UseSqlServer(connectionString));
    }
}
