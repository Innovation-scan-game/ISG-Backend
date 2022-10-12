using IsolatedFunctions.DTO.CardDTOs;

namespace IsolatedFunctions.DTO.GameSessionDTOs;

public class StartGameDto
{
    public List<CardDto> Cards { get; set; } = new();
    public int RoundDuration { get; set; }
}
