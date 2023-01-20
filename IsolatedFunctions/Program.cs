using System.Text.Json;
using DAL.Data;
using Domain.Models;
using FluentValidation;
using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.DTO.GameSessionDTOs;
using IsolatedFunctions.DTO.SignalDTOs;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.DTO.Validators;
using IsolatedFunctions.Infrastructure;
using IsolatedFunctions.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;
using Services.Interfaces;

namespace IsolatedFunctions;

public static class Program
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
                // else
                // {
                //     configurationBuilder.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true,
                //         reloadOnChange: true);
                // }
            })
            .ConfigureFunctionsWorkerDefaults(builder =>
            {
                builder.AddApplicationInsights()
                    .AddApplicationInsightsLogger();

                builder.UseMiddleware<JwtMiddleware>();


                builder.UseWhen<WssMiddleware>(context => { return context.FunctionDefinition.Name == "Negotiate"; });


                builder.Services.AddAutoMapper(typeof(InnovationGameMappingProfile));
                builder.Services.AddSingleton<ITokenService, TokenService>();


                builder.Services.AddOptions<JsonSerializerOptions>()
                    .Configure(options => options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

                builder.Services.AddTransient<IUserService, UserService>();
                builder.Services.AddTransient<ICardService, CardService>();
                builder.Services.AddTransient<ISessionService, SessionService>();
                builder.Services.AddTransient<ISessionResponseService, SessionResponseService>();
                builder.Services.AddTransient<IImageUploadService, ImageUploadService>();
            })
            .ConfigureServices((context, collection) => ConfigureServices(collection, context.Configuration))
            .ConfigureOpenApi()
            .Build();

        await host.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddLogging()
            .AddDbContext<InnovationGameDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("SqlConnectionString")))
            .AddAzureClients(bld => { bld.AddBlobServiceClient(configuration.GetSection("AzureWebJobsStorage")); });

        services.AddScoped<IValidator<Card>, CardValidator>();
        services.AddScoped<IValidator<User>, UserValidator>();
        services.AddScoped<IValidator<GameSession>, GameSessionValidator>();
        services.AddScoped<IValidator<SessionResponse>, SessionResponseValidator>();

    }

}
