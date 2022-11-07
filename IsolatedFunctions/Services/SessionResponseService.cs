using DAL.Data;
using Domain.Models;
using IsolatedFunctions.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IsolatedFunctions.Services;

public class SessionResponseService : ISessionResponseService
{
    private readonly InnovationGameDbContext _context;

    public SessionResponseService(InnovationGameDbContext context)
    {
        _context = context;
    }

    public async Task AddSessionResponse(SessionResponse sessionResponse)
    {
        _context.SessionResponses.Add(sessionResponse);
        await _context.SaveChangesAsync();
    }

    public async Task<SessionResponse?> GetSessionResponse(Guid id)
    {
        return await _context.SessionResponses.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> UserCompletedQuestion(Guid userId, int roundIndex)
    {
        return await _context.SessionResponses.FirstOrDefaultAsync(s => s.User.Id == userId && s.CardNumber == roundIndex) is null;
    }
}
