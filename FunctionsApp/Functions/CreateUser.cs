using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Newtonsoft.Json;

namespace FunctionsApp.Functions;

public class CreateUser
{
    private readonly InnovationGameDbContext _context;
    private readonly IMapper _mapper;

    public CreateUser(InnovationGameDbContext context, IMapper mapper)
    {
        _mapper = mapper;
        _context = context;
    }

    [FunctionName("createUser")]
    [OpenApiRequestBody("application/json", typeof(CreateUserDto), Required = true)]
    [OpenApiOperation(operationId: "CreateUser", tags: new[] {"user"}, Summary = "Creates a new user",
        Description = "Creates a new user based on the data given")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
        CreateUserDto createUserDto;

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        try
        {
            createUserDto = JsonConvert.DeserializeObject<CreateUserDto>(requestBody);
        }
        catch (JsonSerializationException e)
        {
            Console.WriteLine("EXCEPTION: " + e.Message);
            return new BadRequestObjectResult(e.Message);
        }

        List<User> existingUsers = _context.Users.Where(u => u.Name == createUserDto.Username || u.Email == createUserDto.Email).ToList();
        if (existingUsers.Count > 0) return new BadRequestObjectResult("User already exists");

        User user = _mapper.Map<User>(createUserDto);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return new OkObjectResult(user);
    }
}
