using System;

namespace APIContract.DTO;

public class CardDTO
{
    public Guid ID;
    public int CardNumber { get; set; }
    public string CardName { get; set; }
    public string CardBody { get; set; }
    public CardTypeEnum CardType { get; set; }
}
