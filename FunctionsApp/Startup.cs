using System;
using DAL.Data;
using FunctionsApp;
using FunctionsApp.Infrastructure;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace FunctionsApp;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddHttpClient();


        builder.Services.AddAutoMapper(typeof(UserMappingProfile));

        string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        builder.Services.AddDbContext<InnovationGameDbContext>();
        // builder.Services.AddDbContext<InnovationGameDbContext>(
        //     options => options.UseSqlServer(connectionString));
    }
}
