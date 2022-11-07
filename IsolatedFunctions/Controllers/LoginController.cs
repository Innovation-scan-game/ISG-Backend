﻿using System.Net;
using AutoMapper;
using Domain.Models;
using IsolatedFunctions.DTO;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.Extensions;
using IsolatedFunctions.Services;
using IsolatedFunctions.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace IsolatedFunctions.Controllers;

public class LoginController
{
    private readonly IMapper _mapper;
    private IUserService UserService { get; }

    // private InnovationGameDbContext Context { get; }
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
    [OpenApiOperation(operationId: "PostLogin", tags: new[] {"login"}, Summary = "Logs in",
        Description = "A user logs in")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponseDto),
        Description = "the requested user logged in")]
    public async Task<HttpResponseData> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        LoginRequest? login = await req.ReadFromJsonAsync<LoginRequest>();

        if (login == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid login request");
        }

        // User? dbUser = await Context.Users.FirstOrDefaultAsync(u => u.Name == login.Username);

        User? dbUser = await UserService.GetUserByName(login.Username);


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
