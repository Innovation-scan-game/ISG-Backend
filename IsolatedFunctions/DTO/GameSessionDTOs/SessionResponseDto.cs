using IsolatedFunctions.DTO.CardDTOs;

namespace IsolatedFunctions.DTO.GameSessionDTOs;

public class SessionResponseDto
{
    public CardDto[] Cards { get; set; } = Array.Empty<CardDto>();
    public int RoundDuration { get; set; }
}
