﻿using System.Net;
using System.Security.Claims;
using AutoMapper;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Domain.Enums;
using Domain.Models;
using HttpMultipartParser;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.Extensions;
using IsolatedFunctions.Helper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Services;
using Services.Interfaces;

namespace IsolatedFunctions.Controllers;

public sealed class ExampleAuthAttribute : OpenApiSecurityAttribute
{
    public ExampleAuthAttribute() : base("ExampleAuth", SecuritySchemeType.Http)
    {
        Description = "JWT for authorization";
        In = OpenApiSecurityLocationType.Header;
        Scheme = OpenApiSecuritySchemeType.Bearer;
        BearerFormat = "JWT";
    }
}

public class UserController
{
    private readonly ILogger<UserController> _logger;
    private readonly IMapper _mapper;

    private IUserService UserService { get; }
    private readonly IImageUploadService _imageUploadService;

    public UserController(ILoggerFactory loggerFactory, IUserService userService, IMapper mapper, IImageUploadService imageUploadService)
    {
        UserService = userService;
        _logger = loggerFactory.CreateLogger<UserController>();
        _mapper = mapper;
        _imageUploadService = imageUploadService;
    }

    [Function(nameof(GetAllUsers))]
    [OpenApiOperation(operationId: "GetUsers", tags: new[] { "user" }, Summary = "Get all Users",
        Description = "Get a list of all users created")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto),
        Description = "List of all users")]
    public async Task<HttpResponseData> GetAllUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")]
        HttpRequestData req, FunctionContext executionContext)
    {
        _logger.LogInformation($"{executionContext.GetUser()?.Identity?.Name ?? "Someone"} attempted to obtain all users.");
        if (await UserService.CheckUserAllowAdminChange(executionContext.GetUser()!))
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized);

        var users = await UserService.GetAllUsers();

        var userDtos = _mapper.Map<UserDto[]>(users);
        _logger.LogInformation("User request successful.");
        return await req.CreateSuccessResponse(userDtos);
    }

    [Function(nameof(GetUserById))]
    [OpenApiOperation(operationId: "GetUserId", tags: new[] { "user" }, Summary = "Get user by Id",
        Description = "Get a users by ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto),
        Description = "The requested User")]
    public async Task<HttpResponseData> GetUserById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{id}")]
        HttpRequestData req,
        string id)
    {
        _logger.LogInformation($"User requested: {id}");
        User? user;
        if (Guid.TryParse(id, out var userId))
            user = await UserService.GetUser(userId);
        else
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Not a valid id.");

        if (user == null)
            return await req.CreateErrorResponse(HttpStatusCode.NotFound, "User not found.");

        _logger.LogInformation("User Obtained.");
        return await req.CreateSuccessResponse(_mapper.Map<UserDto>(user));
    }

    [Function(nameof(CreateUser))]
    [OpenApiRequestBody("application/json", typeof(CreateUserDto), Required = true)]
    [OpenApiOperation(operationId: "CreateUser", tags: new[] { "user" }, Summary = "Create a new user",
        Description = "Creates a new user based on the data given")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(UserDto), Summary = "The created user",
        Description = "The created user")]
    public async Task<HttpResponseData> CreateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user")]
        HttpRequestData req, FunctionContext executionContext)
    {
        var createUserDto = await req.ReadFromJsonAsync<CreateUserDto>();
        _logger.LogInformation($"Trying to create user: {createUserDto?.Username ?? "[Invalid]"}.");

        if (createUserDto == null)
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request.");

        User createUser = new()
        {
            Name = createUserDto.Username ?? "",
            Email = createUserDto.Email,
            Password = createUserDto.Password,
            Role = UserRoleEnum.User
        };

        if (await UserService.GetExistingUser(createUser.Name, createUser.Email) != null)
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Username or Email already exists.");

        await UserService.AddUser(createUser);
        _logger.LogInformation("User successfully created.");
        return await req.CreateSuccessResponse(createUser);
    }

    [Function(nameof(UploadAvatar))]
    [OpenApiOperation(operationId: "UploadAvatar", tags: new[] { "user" }, Summary = "upload a avatar",
        Description = "Upload a avatar to the user ")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto),
        Description = "The uploaded avatar")]
    public async Task<HttpResponseData> UploadAvatar(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/avatar")]
        HttpRequestData req, FunctionContext executionContext)
    {
        _logger.LogInformation($"Avatar being altered for: {executionContext.GetUser()?.Identity?.Name ?? "Someone"}.");
        if (await UserService.CheckUserLoggedIn(executionContext.GetUser()) is not null)
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "You need to be logged in.");

        var body = await MultipartFormDataParser.ParseAsync(req.Body);
        var file = body.Files[0];

        if (file == null)
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "No file was uploaded.");

        var user = await UserService.GetUserByName(executionContext.GetUser()?.Identity?.Name!);
        user!.Picture = await _imageUploadService.UploadImage(file, Enums.BlobContainerName.ProfileImages);
        await UserService.UpdateUser(user);

        _logger.LogInformation($"Avatar updated for: {user.Name} ({user.Id})");
        return req.CreateResponse(HttpStatusCode.OK);
    }

    [Function(nameof(UpdateUser))]
    [OpenApiOperation(operationId: "UpdateUser", tags: new[] { "user" }, Summary = "Update an existing user",
                Description = "Updates a user ")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto),
                Description = "the updated user")]
    public async Task<HttpResponseData> UpdateUser(
                [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user")]
        HttpRequestData req, FunctionContext executionContext)
    {
        var principal = executionContext.GetUser();
        _logger.LogInformation($"{principal?.Identity?.Name ?? "Unknown user"} being updated.");
        if (principal == null)
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "You need to be logged in to change users.");

        var loggedInUser = await UserService.GetUserByName(principal.Identity!.Name!);
        if (loggedInUser == null)
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid login.");

        var editUser = await req.ReadFromJsonAsync<EditUserDto>();

        User? target;
        if (editUser!.Id == "")
            target = loggedInUser;
        else
        {
            if (!Guid.TryParse(editUser.Id, out var id))
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid user id");

            target = await UserService.GetUser(id);
            if (target == null)
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "User not found");
        }

        if (CheckAuth(loggedInUser, target))
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not authorized to edit this user");

        if (await CheckUsernameOverridden(editUser, loggedInUser))
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "That username is already taken.");

        if (await CheckEmailOverridden(editUser, loggedInUser))
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "That email is already taken.");

        _mapper.Map(editUser, target);
        await UserService.UpdateUser(target);

        var result = _mapper.Map<UserDto>(target);
        _logger.LogInformation($"User {target.Name} ({target.Id}) updated.");
        return await req.CreateSuccessResponse(result);
    }

    [Function(nameof(DeleteUser))]
    [OpenApiOperation(operationId: "DeleteUser", tags: new[] { "user" }, Summary = "Delete the given user",
        Description = "deletes a user by ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto),
        Description = "The deleted user")]
    public async Task<HttpResponseData> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/{id}")]
        HttpRequestData req, FunctionContext executionContext,
        string id)
    {
        _logger.LogInformation($"{executionContext.GetUser()?.Identity?.Name ?? "Unknown user"} being deleted.");
        var loggedInUser = await UserService.CheckUserLoggedIn(executionContext.GetUser());

        if (loggedInUser is null)
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "You need to be logged in to delete users.");

        if (Guid.TryParse(id, out var userId))
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Not a valid guid.");
        var user = await UserService.GetUser(userId);

        if (user == null)
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "User not found");
        if (CheckAuth(loggedInUser, user))
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not authorized to delete other users.");

        await UserService.DeleteUser(user.Id);
        _logger.LogInformation($"User {user.Name} ({user.Id}) deleted.");
        return req.CreateResponse(HttpStatusCode.OK);
    }

    private static bool CheckAuth(User user, User target) => user!.Role != UserRoleEnum.Admin && target.Id != user.Id;
    private async Task<bool> CheckUsernameOverridden(EditUserDto user, User target) => user.Username != target.Name && await UserService.GetUserByName(user.Username) != null;
    private async Task<bool> CheckEmailOverridden(EditUserDto user, User target) => user.Email != target.Email && await UserService.GetUserByEmail(user.Email) != null;
}
