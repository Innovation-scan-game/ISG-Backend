using Domain.Enums;

namespace Domain.Models;

public class SessionResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public GameSession Session { get; set; } = null!;
    public int CardNumber { get; set; }
    public User User { get; set; } = null!;
    public string Response { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public SessionResponseType ResponseType { get; set; } = SessionResponseType.Answer;
}
