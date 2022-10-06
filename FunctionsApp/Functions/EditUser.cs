using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DAL.Data;
using Domain.Models;
using FunctionsApp.DTO.UserDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionsApp.Functions;

public class EditUser
{
    private readonly IMapper _mapper;
    private readonly InnovationGameDbContext _context;

    public EditUser(IMapper mapper, InnovationGameDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    [FunctionName("EditUser")]
    [OpenApiRequestBody("application/json", typeof(EditUserDTO), Required = true)]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = null)]
        HttpRequest req, ILogger log)
    {
        EditUserDTO editUser;
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        try
        {
            editUser = JsonConvert.DeserializeObject<EditUserDTO>(requestBody);
        }
        catch (Exception e)
        {
            return new BadRequestObjectResult(e.Message);
        }

        if (!_context.Users.Any(x => x.Id == Guid.Parse(editUser.Id)))
        {
            return new NotFoundObjectResult("User not found");
        }

        User u = _mapper.Map<User>(editUser);
        _context.Update(u);
        return new OkObjectResult(u);
    }
}
