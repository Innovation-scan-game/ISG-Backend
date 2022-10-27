using System.Text.Json;
using DAL.Data;
using IsolatedFunctions.Infrastructure;
using IsolatedFunctions.Security;
using IsolatedFunctions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;

namespace IsolatedFunctions;

public class Program
{
    public static async Task Main()
    {
        var host = new HostBuilder()
            .ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);


                if (context.HostingEnvironment.IsDevelopment())
                {
                    configurationBuilder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
                }
                else
                {
                    configurationBuilder.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true,
                        reloadOnChange: true);
                }
            })
            .ConfigureFunctionsWorkerDefaults(builder =>
            {
                builder.UseMiddleware<JwtMiddleware>();
                builder.Services.AddDbContext<InnovationGameDbContext>();
                builder.Services.AddAutoMapper(typeof(InnovationGameMappingProfile));
                builder.Services.AddSingleton<ITokenService, TokenService>();
                builder.Services.AddOptions<JsonSerializerOptions>()
                    .Configure(options => options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
            })
            .ConfigureServices((context, collection) => ConfigureServices(collection, context.Configuration))
            .Build();

        await host.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddLogging()
            .AddAzureClients(bld => { bld.AddBlobServiceClient(configuration.GetConnectionString("AzureStorage")); });


        // .AddSingleton()
        // .AddHttpLayer(configuration)
        // .AddKeyVaultLayer(configuration);
    }
}
