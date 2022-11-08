using Domain.Models;

namespace Services.Interfaces;

public interface ISessionService
{
    Task<GameSession?> GetSessionById(Guid id);
    Task AddSession(GameSession session);
    Task<List<GameSession>> GetSessions();
    Task<GameSession?> GetSessionByJoinCode(string joinCode);
}
