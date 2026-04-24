namespace SparkWork2.Models;

public class ConversationItem
{
    public int ParticipantUserId { get; set; }
    public string ParticipantName { get; set; }
    public string LastMessage { get; set; }
    public DateTime LastMessageDate { get; set; }
}