
namespace Domain.Models;

public class SessionResponse
{
    public Guid Id { get; set; }
    public GameSession Session { get; set; }
    public int CardNumber { get; set; }
    public User User { get; set; }
    public string response { get; set; }
    public DateTime CreatedAt { get; set; }

}
