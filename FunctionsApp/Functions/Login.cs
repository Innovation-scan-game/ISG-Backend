using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using DAL.Data;
using Domain.Models;
using FunctionsApp.DTO;
using FunctionsApp.DTO.UserDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;

namespace FunctionsApp.Functions;

public class Login
{
    private readonly InnovationGameDbContext _context;
    private readonly IMapper _mapper;

    public Login(InnovationGameDbContext context, IMapper mapper)

    {
        _context = context;
        _mapper = mapper;
    }

    [FunctionName("login")]
    [OpenApiRequestBody("application/json", typeof(CreateUserDTO), Required = true)]
    [OpenApiOperation(operationId: "LoginUser", tags: new[] {"user"}, Summary = "A login for the user",
        Description = "A user can login based on their ID by using their token")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponseDTO),
        Description = "User authorization token")]
    public async Task<IActionResult> LoginUser([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var createUser = JsonConvert.DeserializeObject<CreateUserDTO>(requestBody);
        var user = _mapper.Map<User>(createUser);
        var existingUser = _context.Users.FirstOrDefault(u => u.Name == user.Name);

        if (existingUser == null)
        {
            return new BadRequestObjectResult("User does not exist");
        }

        if (BCrypt.Net.BCrypt.Verify(createUser.Password, existingUser?.Password))
        {
            // Send login token
            return new OkObjectResult("valid login");
        }

        return new BadRequestObjectResult("invalid password");
    }
}
