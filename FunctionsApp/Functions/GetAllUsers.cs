using System;
using System.Linq;
using System.Net;
using AutoMapper;
using DAL.Data;
using IsolatedFunctions.DTO.UserDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace FunctionsApp.Functions;

public class GetAllUsers
{
    private readonly InnovationGameDbContext _context;
    private readonly IMapper _mapper;

    public GetAllUsers(InnovationGameDbContext context, IMapper mapper)

    {
        _mapper = mapper;
        _context = context;
    }

    [FunctionName("getAllUsers")]
    [OpenApiOperation(operationId: "UserList", tags: new[] {"user"}, Summary = "Gets user list", Description = "Gets list of users")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto[]))]
    public IActionResult UserList([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
    {
        var users = _context.Users.ToList();

        var userDtos = users.Select(u => _mapper.Map<UserDto>(u)).ToArray();

        Console.WriteLine("userdtos");
        // UserDTO user1 = new() {Username = "jurek", Email = "jurek.baumann@gmail.com", Role = UserRoleEnum.Admin};
        // UserDTO user2 = new() {Username = "John Doe", Email = "john@doe.com", Role = UserRoleEnum.User};
        // UserDTO user3 = new() {Username = "Hans", Email = "hans@gmail.com", Role = UserRoleEnum.User};
        // UserDTO user4 = new() {Username = "Random Guy", Email = "rnd@gmail.com", Role = UserRoleEnum.User};
        //
        // return new OkObjectResult(new[] {user1, user2, user3, user4});
        return new OkObjectResult(userDtos);
    }
}
