namespace SparkWork2.Models;

public class ConversationItem
{
    public int ParticipantUserId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageDate { get; set; }

    public string ParticipantInitial =>
        string.IsNullOrWhiteSpace(ParticipantName)
            ? "?"
            : ParticipantName.Trim()[0].ToString().ToUpperInvariant();
}
