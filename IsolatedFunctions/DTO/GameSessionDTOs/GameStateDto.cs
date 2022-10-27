using IsolatedFunctions.DTO.CardDTOs;

namespace IsolatedFunctions.DTO.GameSessionDTOs;

// TODO: remove
public class GameStateDto
{
    public int GameRound { get; set; }
    public CardDto[]? Cards { get; set; }
    public int PlayerCount { get; set; }
}
