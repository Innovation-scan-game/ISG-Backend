﻿using System.Net;
using System.Security.Claims;
using AutoMapper;
using DAL.Data;
using Domain.Enums;
using Domain.Models;
using IsolatedFunctions.DTO;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

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
    private readonly ILogger<LoginController> _logger;
    private readonly InnovationGameDbContext _context;
    private readonly IMapper _mapper;

    public UserController(ILoggerFactory loggerFactory, InnovationGameDbContext context, IMapper mapper)
    {
        _context = context;
        _logger = loggerFactory.CreateLogger<LoginController>();
        _mapper = mapper;
    }

    [Function(nameof(UploadProfilePicture))]
    public async Task<HttpResponseData> UploadProfilePicture(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/uploadpicture")]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        return response;
    }

    [Function(nameof(GetAllUsers))]
    public async Task<UserDto[]> GetAllUsers([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "userList")] HttpRequestData req)
    {
        List<User> users = await _context.Users.ToListAsync();
        return _mapper.Map<UserDto[]>(users);
    }

    [Function(nameof(GetUserById))]
    public async Task<UserDto> GetUserById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{id}")] HttpRequestData req,
        string id)
    {
        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));
        return _mapper.Map<UserDto>(user);
    }

    [Function(nameof(CreateUser))]
    [OpenApiRequestBody("application/json", typeof(CreateUserDto), Required = true)]
    [OpenApiOperation(operationId: "CreateUser", tags: new[] {"user"}, Summary = "Creates a new user",
        Description = "Creates a new user based on the data given")]
    public async Task<HttpResponseData> CreateUser([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req)
    {
        HttpResponseData response = req.CreateResponse();

        CreateUserDto? createUserDto = await req.ReadFromJsonAsync<CreateUserDto>();

        User? existing = _context.Users.FirstOrDefault(u => u.Name == createUserDto.Username || u.Email == createUserDto.Email);

        if (existing != null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(new ErrorDto {Message = "User already exists!"});
            return response;
        }

        User user = _mapper.Map<User>(createUserDto);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        response.StatusCode = HttpStatusCode.OK;
        await response.WriteAsJsonAsync(createUserDto);
        return response;
    }

    [Function(nameof(UpdateUser))]
    public async Task<HttpResponseData> UpdateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/edit")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? principal = executionContext.GetUser();

        if (principal == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "You need to be logged in to change users.");
        }
        User? loggedInUser = _context.Users.FirstOrDefault(u => u.Name == principal.Identity!.Name);
        EditUserDto? editUser = await req.ReadFromJsonAsync<EditUserDto>();

        User? target;
        if (editUser!.Id == "")
        {
            target = loggedInUser!;
        }
        else
        {
            if (!Guid.TryParse(editUser.Id, out Guid id))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid user id");
            }

            target = _context.Users.FirstOrDefault(u => u.Id == id);
            if (target == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "User not found");
            }
        }

        if (loggedInUser!.Role != UserRoleEnum.Admin && target.Id != loggedInUser.Id)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not authorized to edit this user");
        }


        if (_context.Users.Any(u => u.Name == editUser.Username))
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "That username is already taken.");
        }

        if (_context.Users.Any(u => u.Email == editUser.Email))
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "That email is already taken.");
        }

        _mapper.Map(editUser, target);
        await _context.SaveChangesAsync();

        var result = _mapper.Map<UserDto>(target);
        return await req.CreateSuccessResponse(result);
    }
}
