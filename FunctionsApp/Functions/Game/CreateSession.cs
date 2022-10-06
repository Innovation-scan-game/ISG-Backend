using System.Threading.Tasks;
using DAL.Data;
using Domain.Models;
using FunctionsApp.DTO.GameSessionDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FunctionsApp.Functions.Game;

public class CreateSession
{
    private readonly InnovationGameDbContext _context;

    public CreateSession(InnovationGameDbContext context)
    {
        _context = context;
    }

    [FunctionName("CreateSession")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
        HttpRequest req, ILogger log)
    {
        GameSession session = GameSession.New();
        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();
        return new OkObjectResult(new LobbyResponseDTO {SessionAuth = session.SessionCode});
    }
}
