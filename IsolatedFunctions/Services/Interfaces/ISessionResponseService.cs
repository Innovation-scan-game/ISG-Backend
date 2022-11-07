using Domain.Models;

namespace IsolatedFunctions.Services.Interfaces;

public interface ISessionResponseService
{
    Task AddSessionResponse(SessionResponse sessionResponse);
    Task<SessionResponse?> GetSessionResponse(Guid id);
}
