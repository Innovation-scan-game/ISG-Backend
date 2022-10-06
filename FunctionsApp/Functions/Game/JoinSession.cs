using System;
using System.IO;
using System.Threading.Tasks;
using DAL.Data;
using FunctionsApp.DTO.GameSessionDTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionsApp.Functions.Game;

public class JoinSession
{
    private readonly InnovationGameDbContext _context;

    public JoinSession(InnovationGameDbContext context)
    {
        _context = context;
    }

    [FunctionName("JoinSession")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequest req, ILogger log)
    {
        try
        {
            JoinRequestDTO joinRequestDto =
                JsonConvert.DeserializeObject<JoinRequestDTO>(await new StreamReader(req.Body).ReadToEndAsync());

            var session = await _context.GameSessions.FirstOrDefaultAsync(session => session.SessionCode == joinRequestDto.SessionAuth);

            if (session == null)
            {
                return new BadRequestObjectResult("Session not found");
            }

            return new OkObjectResult(session.Id);
        }
        catch (JsonSerializationException e)
        {
            return new BadRequestObjectResult(e.Message);
        }
    }
}
