using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Models;

public class Card
{
    public Guid Id { get; set; }

    public int CardNumber { get; set; }
    public string Name { get; set; } = "";

    public string? Picture { get; set; } = null;

    [Column(TypeName = "ntext")] public string Body { get; set; } = "";
    public CardTypeEnum Type { get; set; }
    public virtual ICollection<GameSession> Sessions { get; set; } = new List<GameSession>();
}
