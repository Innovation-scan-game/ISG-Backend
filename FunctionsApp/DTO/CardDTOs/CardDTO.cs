using System;
using Domain.Enums;

namespace FunctionsApp.DTO.CardDTOs;

public class CardDTO
{
    public Guid ID;
    public int CardNumber { get; set; }
    public string CardName { get; set; }
    public string CardBody { get; set; }
    public CardTypeEnum CardType { get; set; }
}
