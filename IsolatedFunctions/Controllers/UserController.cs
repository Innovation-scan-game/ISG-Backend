using System.Net;
using AutoMapper;
using DAL.Data;
using Domain.Models;
using IsolatedFunctions.DTO;
using IsolatedFunctions.DTO.UserDTOs;
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/uploadpicture")] HttpRequestData req,
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
    public async Task<UserDto> GetUserById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{id}")] HttpRequestData req, string id)
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
}
