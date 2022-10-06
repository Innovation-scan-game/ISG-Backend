using System;
using FunctionsApp.DTO.CardDTOs;

namespace FunctionsApp.DTO;

public class MatchHistoryEntry {
    public int ID { get; set; }
    public DateTime date { get; set; }
    public CardDTO card { get; set; }
    public string textResponse { get; set; }
    public string textEmoji { get; set; }
}
