using System.Security.Cryptography;
using System.Text;
using Domain.Enums;

namespace Domain.Models;

public class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SessionCode { get; set; } = "";
    public DateTime Created { get; set; }

    public SessionStatus Status { get; set; }
    // public Guid HostId { get; set; }

    // one to one
    public Guid HostId { get; set; }

    // public virtual User Host { get; set; }
    public int RoundDurationSeconds { get; set; }
    public int Rounds { get; set; }
    public int CurrentRound { get; set; }
    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();

    public ICollection<User> Players { get; set; } = new List<User>();

    public ICollection<SessionResponse> Responses { get; set; } = new List<SessionResponse>();

    public static GameSession New()
    {
        Guid id = Guid.NewGuid();

        return new GameSession
        {
            Id = id,
            Created = DateTime.Now,
            Status = SessionStatus.Lobby,
            SessionCode = GenerateSessionCode(id)
        };
    }

    /// <summary>
    /// Generate a random 4 character string
    /// </summary>
    private static string GenerateSessionCode(Guid id)
    {
        byte[] hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(id.ToString()));
        return string.Concat(hash.Select(b => b.ToString("x2")))[..4];
    }
}
