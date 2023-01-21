using DAL.Data;
using Domain.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;

namespace Services;

public class SessionResponseService : ISessionResponseService
{
    private readonly InnovationGameDbContext _context;
    private readonly IValidator<SessionResponse> _validator;

    public SessionResponseService(InnovationGameDbContext context, IValidator<SessionResponse> validator)
    {
        _context = context;
        _validator = validator;
    }


    public async Task AddSessionResponse(SessionResponse sessionResponse)
    {
        var validation = await _validator.ValidateAsync(sessionResponse);
        if (!validation.IsValid)
        {
            throw new Exception(validation.Errors[0].ErrorMessage);
        }
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
