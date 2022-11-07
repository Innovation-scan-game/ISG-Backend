using DAL.Data;
using Domain.Enums;
using Domain.Models;
using IsolatedFunctions.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IsolatedFunctions.Services;

public class SessionService : ISessionService
{
    private readonly InnovationGameDbContext _context;

    public SessionService(InnovationGameDbContext context)
    {
        _context = context;
    }

    public async Task<GameSession?> GetSessionByJoinCode(string joinCode)
    {
        return await _context.GameSessions.Include(s => s.Players)
            .FirstOrDefaultAsync(session => session.SessionCode == joinCode && session.Status == SessionStatus.Lobby);
    }


    public async Task<GameSession?> GetSessionById(Guid id)
    {
        return await _context.GameSessions.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddSession(GameSession session)
    {
        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();
    }

    public async Task<List<GameSession>> GetSessions()
    {
        return await _context.GameSessions.Include(s => s.Players).Include(s => s.Cards).Include(s => s.Responses)
            .ThenInclude(r => r.User).ToListAsync();
    }
}
