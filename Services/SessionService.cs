using DAL.Data;
using Domain.Enums;
using Domain.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;

namespace Services;

public class SessionService : ISessionService
{
    private readonly InnovationGameDbContext _context;
    private readonly IValidator<GameSession> _validator;

    public SessionService(InnovationGameDbContext context, IValidator<GameSession> validator)

    {
        _context = context;
        _validator = validator;
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
        var validation = await _validator.ValidateAsync(session);
        if (!validation.IsValid)
        {
            throw new Exception(validation.Errors[0].ErrorMessage);
        }

        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();
    }

    public async Task<List<GameSession>> GetSessions()
    {
        return await _context.GameSessions.Include(s => s.Players).Include(s => s.Cards).Include(s => s.Responses)
            .ThenInclude(r => r.User).ToListAsync();
    }
}
