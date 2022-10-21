using System.Net;
using System.Security.Claims;
using AutoMapper;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DAL.Data;
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

    private readonly BlobContainerClient _blobContainerClient;

    public UserController(ILoggerFactory loggerFactory, InnovationGameDbContext context, IMapper mapper,
        BlobServiceClient blobServiceClient)
    {
        _context = context;
        _logger = loggerFactory.CreateLogger<LoginController>();
        _mapper = mapper;
        _blobContainerClient = blobServiceClient.GetBlobContainerClient("profile-pictures");
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
    public async Task<HttpResponseData> GetAllUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/all")]
        HttpRequestData req)
    {
        List<User> users = await _context.Users.ToListAsync();
        var userDtos = _mapper.Map<UserDto[]>(users);

        return await req.CreateSuccessResponse(userDtos);
    }

    [Function(nameof(GetUserById))]
    public async Task<HttpResponseData> GetUserById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{id}")]
        HttpRequestData req,
        string id)
    {
        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        return await req.CreateSuccessResponse(_mapper.Map<UserDto>(user));
    }

    [Function(nameof(CreateUser))]
    [OpenApiRequestBody("application/json", typeof(CreateUserDto), Required = true)]
    [OpenApiOperation(operationId: "CreateUser", tags: new[] {"user"}, Summary = "Creates a new user",
        Description = "Creates a new user based on the data given")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(UserDto), Summary = "The created user",
        Description = "The created user")]
    public async Task<HttpResponseData> CreateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/register")]
        HttpRequestData req, FunctionContext executionContext)

    {
        CreateUserDto? createUserDto = await req.ReadFromJsonAsync<CreateUserDto>();

        if (createUserDto == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request.");
        }

        User? existing = _context.Users.FirstOrDefault(u => u.Name == createUserDto.Username || u.Email == createUserDto.Email);

        if (existing != null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "User already exists");
        }

        User user = _mapper.Map<User>(createUserDto);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return await req.CreateSuccessResponse(createUserDto);
    }

    [Function(nameof(UploadAvatar))]
    public async Task<HttpResponseData> UploadAvatar(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/avatar")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? principal = executionContext.GetUser();

        if (principal == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "You need to be logged in.");
        }

        User? loggedInUser = _context.Users.FirstOrDefault(u => u.Name == principal.Identity!.Name);

        var body = await MultipartFormDataParser.ParseAsync(req.Body);

        FilePart? file = body.Files.First();

        if (file == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "No file was uploaded.");
        }


        if (file.ContentType != "image/png" && file.ContentType != "image/jpeg")
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid image file format: {file.ContentType}");
        }

        string ext = file.ContentType == "image/png" ? ".png" : ".jpg";

        Stream s = Helpers.ResizeImage(file);
        var md5 = Helpers.GenerateMd5Hash(s);

        BlobClient blob = _blobContainerClient.GetBlobClient(md5 + ext);
        s.Position = 0;

        await blob.UploadAsync(s, new BlobHttpHeaders {ContentType = file.ContentType});

        loggedInUser!.Picture = blob.Uri.ToString();

        await _context.SaveChangesAsync();

        Console.WriteLine(file.ContentType);

        Console.WriteLine("upload");


        HttpResponseData response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
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


        if (editUser.Username != loggedInUser.Name && _context.Users.Any(u => u.Name == editUser.Username))
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "That username is already taken.");
        }

        if (editUser.Email != loggedInUser.Email && _context.Users.Any(u => u.Email == editUser.Email))
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "That email is already taken.");
        }

        _mapper.Map(editUser, target);
        await _context.SaveChangesAsync();

        var result = _mapper.Map<UserDto>(target);
        return await req.CreateSuccessResponse(result);
    }

    [Function(nameof(DeleteUser))]
    public async Task<HttpResponseData> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/{id}")]
        HttpRequestData req, FunctionContext executionContext,
        string id)

    {
        ClaimsPrincipal? principal = executionContext.GetUser();

        if (principal == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "You need to be logged in to delete users.");
        }

        User? loggedInUser = _context.Users.FirstOrDefault(u => u.Name == principal.Identity!.Name);

        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (user == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "User not found");
        }

        if (loggedInUser!.Role != UserRoleEnum.Admin && user.Id != loggedInUser.Id)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not authorized to delete other users.");
        }


        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return req.CreateResponse(HttpStatusCode.OK);
    }
}
