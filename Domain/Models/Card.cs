using Domain.Enums;

namespace Domain.Models;

public class Card
{
    public Guid Id { get; set; }
    public int CardNumber { get; set; }
    public string Name { get; set; }
    public string Body { get; set; }
    public CardTypeEnum Type { get; set; }
}
