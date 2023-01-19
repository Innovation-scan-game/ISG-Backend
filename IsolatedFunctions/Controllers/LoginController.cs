using System.Net;
using AutoMapper;
using Domain.Models;
using IsolatedFunctions.DTO;
using IsolatedFunctions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Services;
using Services.Interfaces;

namespace IsolatedFunctions.Controllers;

public class LoginController
{
    private readonly IMapper _mapper;
    private IUserService UserService { get; }

    private ILogger Logger { get; }
    private ITokenService TokenService { get; }

    public LoginController(ITokenService tokenService, ILogger<LoginController> logger, IMapper mapper, IUserService userService)
    {
        TokenService = tokenService;
        Logger = logger;
        _mapper = mapper;
        UserService = userService;
    }

    [Function(nameof(Login))]
    [OpenApiOperation(operationId: "PostLogin", tags: new[] {"login"}, Summary = "Endpoint to log in",
        Description = "A user logs in")]
    [OpenApiRequestBody("application/json", typeof(LoginRequest), Description = "User login data")]

    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponseDto),
        Description = "the requested user logged in")]
    public async Task<HttpResponseData> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "login")] HttpRequestData req)
    {
        LoginRequest? login = await req.ReadFromJsonAsync<LoginRequest>();

        if (login is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid login request!");
        }


        User? dbUser = await UserService.GetUserByName(login.Username);

        if (dbUser is null || !ValidatePassword(login.Password, dbUser.Password))
        {
            return await req.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid username or password.");
        }

        Logger.LogInformation("User {DbUserName} logged in", dbUser.Name);
        LoginResult result = await TokenService.CreateToken(dbUser);

        return await req.CreateSuccessResponse(result);
    }

    private static bool ValidatePassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
