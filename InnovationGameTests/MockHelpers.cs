using System.Text;
using AutoMapper;
using Azure.Core.Serialization;
using DAL.Data;
using InnovationGameTests.DTOs;
using IsolatedFunctions.Controllers;
using IsolatedFunctions.DTO.Validators;
using IsolatedFunctions.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Services;

namespace InnovationGameTests;

public static class MockHelpers
{
    public static HttpRequestData CreateHttpRequestData(string? payload = null,
        string? token = null,
        string method = "GET")
    {
        // use middleware

        var input = payload ?? string.Empty;
        var functionContext = CreateContext(new NewtonsoftJsonObjectSerializer(), token);
        var request = new MockHttpRequestData(functionContext, method: method,
            body: new MemoryStream(Encoding.UTF8.GetBytes(input)));
        request.Headers.Add("Content-Type", "application/json");
        if (token != null)
        {
            request.Headers.Add("Authorization", $"Bearer {token}");
        }

        return request;
    }

    private static FunctionContext CreateContext(ObjectSerializer? serializer = null, string? token = null)
    {
        var context = new MockFunctionContext(token);

        var services = new ServiceCollection();

        services.AddFunctionsWorkerCore();

        services.Configure<WorkerOptions>(c => { c.Serializer = serializer; });

        context.InstanceServices = services.BuildServiceProvider();

        return context;
    }

    public static IMapper CreateMapper()
    {
        MapperConfiguration mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile(new InnovationGameMappingProfile()); });
        IMapper? mapper = mapperConfig.CreateMapper();
        return mapper;
    }

    public static TokenService CreateTokenService()
    {
        var logger = new Mock<ILogger<TokenService>>();
        return new TokenService(null!, logger.Object);
    }

    public static InnovationGameDbContext CreateDbContext()
    {
        DbContextOptions<InnovationGameDbContext> options = new DbContextOptionsBuilder<InnovationGameDbContext>()
            .UseInMemoryDatabase(databaseName: "InnovationGameDB")
            .Options;

        return new InnovationGameDbContext(options);
    }

    public static async Task<string> GetLoginToken(string username, string password)
    {
        Mock<ILogger<LoginController>> logger = new();
        LoginController loginController = new(CreateTokenService(), logger.Object, CreateMapper(), new UserService(CreateDbContext(), new UserValidator()));

        LoginRequest loginRequest = new()
        {
            Username = username,
            Password = password
        };
        string json = JsonConvert.SerializeObject(loginRequest);

        HttpRequestData req = CreateHttpRequestData(json);

        var res = await loginController.Login(req);

        // read response body

        res.Body.Position = 0;
        var reader = new StreamReader(res.Body);
        var responseBod = await reader.ReadToEndAsync();

        LoginResultDto result = JsonConvert.DeserializeObject<LoginResultDto>(responseBod);
        return result.AccessToken;
    }
}
