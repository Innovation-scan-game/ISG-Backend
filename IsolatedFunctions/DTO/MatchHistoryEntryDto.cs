using IsolatedFunctions.DTO.CardDTOs;

namespace IsolatedFunctions.DTO;

public class MatchHistoryEntryDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public CardDto Card { get; set; }
    public string TextResponse { get; set; } = "";
    public string TextEmoji { get; set; } = "";
}
