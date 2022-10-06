
using FunctionsApp.DTO.CardDTOs;

namespace FunctionsApp.DTO.GameSessionDTOs;

public class GameStateDTO
{
    public int GameRound { get; set; }
    public CardDTO[] Cards { get; set; }
    public int PlayerCount { get; set; }
}
