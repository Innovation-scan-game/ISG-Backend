namespace IsolatedFunctions.DTO;

public class HistoryDto
{
    public int Id { get; set; }
    public MatchHistoryEntryDto[] HistoryEntries { get; set; } = Array.Empty<MatchHistoryEntryDto>();
}
