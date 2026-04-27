using SQLite;

namespace SparkWork2.Models;

public class Message
{
    [PrimaryKey, AutoIncrement]
    public int MessageId { get; set; }

    public int SenderUserId { get; set; }
    public int ReceiverUserId { get; set; }

    public string SenderName { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }

    [Ignore]
    public bool IsMine { get; set; }
}