using SparkWork2.Models;
using SparkWork2.Services;

namespace SparkWork2.Repositories;

public class MessageRepository
{
    private readonly DatabaseService _databaseService;

    public MessageRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task SeedDataAsync()
    {
        var db = await _databaseService.GetConnectionAsync();

        var existingMessages = await db.Table<Message>().ToListAsync();
        if (existingMessages.Any())
            return;

        var seedMessages = new List<Message>
        {
            new Message
            {
                SenderUserId = 2,
                ReceiverUserId = 1,
                SenderName = "TechCorp",
                ReceiverName = "Adel Chfik",
                Content = "Hello, we are interested in your profile.",
                SentAt = DateTime.Now.AddMinutes(-30)
            },
            new Message
            {
                SenderUserId = 3,
                ReceiverUserId = 1,
                SenderName = "StartUp Vision",
                ReceiverName = "Adel Chfik",
                Content = "Can we schedule an interview?",
                SentAt = DateTime.Now.AddHours(-2)
            }
        };

        await db.InsertAllAsync(seedMessages);
    }

    public async Task<List<Message>> GetConversationAsync(int currentUserId, int participantUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var allMessages = await db.Table<Message>().ToListAsync();

        return allMessages
            .Where(x =>
                (x.SenderUserId == currentUserId && x.ReceiverUserId == participantUserId) ||
                (x.SenderUserId == participantUserId && x.ReceiverUserId == currentUserId))
            .OrderBy(x => x.SentAt)
            .ToList();
    }

    public async Task<List<ConversationItem>> GetConversationsAsync(int currentUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var allMessages = await db.Table<Message>().ToListAsync();

        return allMessages
            .Where(x => x.SenderUserId == currentUserId || x.ReceiverUserId == currentUserId)
            .Select(message => new
            {
                ParticipantUserId = message.SenderUserId == currentUserId
                    ? message.ReceiverUserId
                    : message.SenderUserId,

                ParticipantName = message.SenderUserId == currentUserId
                    ? message.ReceiverName
                    : message.SenderName,

                Message = message
            })
            .GroupBy(x => x.ParticipantUserId)
            .Select(group =>
            {
                var lastMessage = group
                    .OrderByDescending(x => x.Message.SentAt)
                    .First();

                return new ConversationItem
                {
                    ParticipantUserId = group.Key,
                    ParticipantName = lastMessage.ParticipantName,
                    LastMessage = lastMessage.Message.Content,
                    LastMessageDate = lastMessage.Message.SentAt
                };
            })
            .OrderByDescending(x => x.LastMessageDate)
            .ToList();
    }

    public async Task AddMessageAsync(
        int senderUserId,
        int receiverUserId,
        string senderName,
        string receiverName,
        string content)
    {
        var db = await _databaseService.GetConnectionAsync();

        var message = new Message
        {
            SenderUserId = senderUserId,
            ReceiverUserId = receiverUserId,
            SenderName = senderName,
            ReceiverName = receiverName,
            Content = content,
            SentAt = DateTime.Now
        };

        await db.InsertAsync(message);
    }

    public async Task DeleteConversationAsync(int currentUserId, int participantUserId)
    {
        var db = await _databaseService.GetConnectionAsync();

        var allMessages = await db.Table<Message>().ToListAsync();

        var messagesToDelete = allMessages
            .Where(x =>
                (x.SenderUserId == currentUserId && x.ReceiverUserId == participantUserId) ||
                (x.SenderUserId == participantUserId && x.ReceiverUserId == currentUserId))
            .ToList();

        foreach (var message in messagesToDelete)
        {
            await db.DeleteAsync(message);
        }
    }
}