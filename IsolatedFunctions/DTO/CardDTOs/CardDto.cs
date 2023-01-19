using Domain.Enums;

namespace IsolatedFunctions.DTO.CardDTOs;

public class CardDto
{
    public Guid Id { get; set; }
    public int CardNumber { get; set; }
    public string CardName { get; set; } = "";
    public string CardBody { get; set; } = "";
    public string Picture { get; set; } = "";
    public CardTypeEnum Type { get; set; }
}
