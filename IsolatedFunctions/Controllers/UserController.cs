using System.Net;
using AutoMapper;
using DAL.Data;
using Domain.Models;
using IsolatedFunctions.DTO.UserDTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

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

    private T GetBody<T>(HttpRequestData request)
    {
        string json = new StreamReader(request.Body).ReadToEnd();
        return JsonConvert.DeserializeObject<T>(json);
    }

    [Function("createUser")]
    [OpenApiRequestBody("application/json", typeof(CreateUserDto), Required = true)]
    [OpenApiOperation(operationId: "CreateUser", tags: new[] {"user"}, Summary = "Creates a new user",
        Description = "Creates a new user based on the data given")]
    public async Task<HttpResponseData> CreateUser([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req)
    {
        // CreateUserDTO createUserDto;
        HttpResponseData response = req.CreateResponse();

        CreateUserDto createUserDto = GetBody<CreateUserDto>(req);

        User? existing = _context.Users.FirstOrDefault(u => u.Name == createUserDto.Username || u.Email == createUserDto.Email);

        if (existing != null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync("User already exists!");
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
