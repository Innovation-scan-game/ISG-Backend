namespace IsolatedFunctions.DTO.CardDTOs;

public class CardResponseDto
{
    public int CardNumber { get; set; }
    public string SessionAuth { get; set; } = "";
    public string ResponseText { get; set; } = "";
}
