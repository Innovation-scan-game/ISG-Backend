using System;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using DAL.Data;
using Domain.Models;
using IsolatedFunctions.DTO.UserDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace FunctionsApp.Functions;

public class GetUser
{
    private readonly InnovationGameDbContext _context;
    private readonly IMapper _mapper;

    public GetUser(InnovationGameDbContext context, IMapper mapper)
    {
        _mapper = mapper;
        _context = context;
    }

    [FunctionName("userInfo")]
    [OpenApiOperation(operationId: "UserInfo", tags: new[] {"user"}, Summary = "Gets user info",
        Description = "Gets information about user by user ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto))]
    public async Task<IActionResult> UserInfo([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "userInfo/{id}")] HttpRequest req,
        string id)
    {
        if (!Guid.TryParse(id, out Guid userId))
        {
            return new BadRequestObjectResult("Invalid user ID");
        }

        User u = await _context.Users.FindAsync(userId);
        if (u == null)
        {
            return new NotFoundObjectResult("User not found");
        }

        UserDto dto = _mapper.Map<UserDto>(u);
        return new OkObjectResult(dto);
    }
}
