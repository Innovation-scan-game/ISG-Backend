using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public UserRoleEnum Role { get; set; }

    [ForeignKey("SessionId")]
    public virtual GameSession? CurrentSession { get; set; }
    public Guid? SessionId { get; set; }
    public bool Ready { get; set; }
    public string Picture { get; set; } = "";
}
