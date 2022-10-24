using System.Net;
using AutoMapper;
using DAL.Data;
using Domain.Models;
using IsolatedFunctions.DTO;
using IsolatedFunctions.DTO.GameSessionDTOs;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.Extensions;
using IsolatedFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IsolatedFunctions.Controllers;

public class LoginController
{
    private readonly IMapper _mapper;
    private InnovationGameDbContext Context { get; }
    private ILogger Logger { get; }
    private ITokenService TokenService { get; }

    public LoginController(ITokenService tokenService, ILogger<LoginController> logger, InnovationGameDbContext context, IMapper mapper)
    {
        TokenService = tokenService;
        Logger = logger;
        Context = context;
        _mapper = mapper;
    }

    [Function(nameof(Login))]
    [OpenApiOperation(operationId: "PostLogin", tags: new[] {"login"}, Summary = "Logs in",
        Description = "A user logs in")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponseDto),
        Description = "the requested user logged in")]
    public async Task<HttpResponseData> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        LoginRequest? login = await req.ReadFromJsonAsync<LoginRequest>();

        if (login == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request");
        }

        User? dbUser = await Context.Users.FirstOrDefaultAsync(u => u.Name == login.Username);

        if (dbUser == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.NotFound, "User not found");
        }

        if (!BCrypt.Net.BCrypt.Verify(login.Password, dbUser.Password))
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid password");
        }

        UserDto userDto = _mapper.Map<UserDto>(dbUser);

        Logger.LogInformation("User {DbUserName} logged in", dbUser.Name);
        LoginResult result = await TokenService.CreateToken(userDto);


        return await req.CreateSuccessResponse(result);
    }
}
