using System.Net;
using DAL.Data;
using Domain.Models;
using IsolatedFunctions.DTO;
using IsolatedFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IsolatedFunctions.Controllers;

public class LoginController
{
    private InnovationGameDbContext Context { get; }
    private ILogger Logger { get; }
    private ITokenService TokenService { get; }

    public LoginController(ITokenService tokenService, ILogger<LoginController> logger, InnovationGameDbContext context)
    {
        TokenService = tokenService;
        Logger = logger;
        Context = context;
    }

    [Function(nameof(Login))]
    public async Task<HttpResponseData> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        LoginRequest? login = await req.ReadFromJsonAsync<LoginRequest>();

        HttpResponseData response = req.CreateResponse();


        if (login == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(new ErrorDto {Message = "Invalid request"});
            return response;
        }

        User? dbUser = await Context.Users.FirstOrDefaultAsync(u => u.Name == login.Username);

        if (dbUser == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            await response.WriteAsJsonAsync(new ErrorDto {Message = "User not found"});
            return response;
        }

        if (!BCrypt.Net.BCrypt.Verify(login.Password, dbUser.Password))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(new ErrorDto {Message = "Invalid password"});
            return response;
        }

        Logger.LogInformation("User {DbUserName} logged in", dbUser.Name);
        LoginResult result = await TokenService.CreateToken(dbUser);

        response.StatusCode = HttpStatusCode.OK;
        await response.WriteAsJsonAsync(result);
        return response;
    }
}
