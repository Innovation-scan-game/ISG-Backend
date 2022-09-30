namespace APIContract.DTO;

class HistoryDTO {
    public int ID { get; set; }
    public MatchHistoryEntry[] HistoryEntries { get; set; }
}
