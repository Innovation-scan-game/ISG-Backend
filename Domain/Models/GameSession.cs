using System.Globalization;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using Domain.Enums;

namespace Domain.Models;

public class GameSession
{
    public Guid Id { get; set; }
    public string SessionCode { get; set; }
    public DateTime Created { get; set; }
    public SessionStatus Status { get; set; }
    public User Host { get; set; }

    public static GameSession New()
    {
        Guid id = Guid.NewGuid();

        return new GameSession
        {
            Id = id,
            Created = DateTime.Now,
            Status = SessionStatus.Active,
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
