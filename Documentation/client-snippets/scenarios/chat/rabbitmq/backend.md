```csharp
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Concepts;

namespace MyApp.Chat;

// Chat/IChatPersistence.cs
public interface IChatPersistence
{
    // Loads a room's messages, oldest first.
    Task<IEnumerable<ChatMessage>> GetHistoryAsync(string roomName);
}

// Chat/ChatRoom.cs
public class ChatRoom(IEnumerable<ChatMessage> history)
{
    readonly object _lock = new();
    readonly List<ChatMessage> _history = [.. history];

    public BehaviorSubject<IEnumerable<ChatMessage>> Messages { get; } = new([.. history]);

    // Called for every message that arrives from the broker.
    public void Receive(ChatMessage message)
    {
        lock (_lock)
        {
            _history.Add(message);
            Messages.OnNext([.. _history]);
        }
    }
}

// Register as a singleton: builder.Services.AddSingleton<ChatService>();
public class ChatService(IChatPersistence persistence)
{
    readonly ConcurrentDictionary<string, Lazy<ChatRoom>> _rooms = new();

    // Loads the history once, the first time a room is asked for.
    public ChatRoom GetChatRoom(string name)
    {
        var room = _rooms.GetOrAdd(name, roomName => new Lazy<ChatRoom>(() =>
            new ChatRoom(persistence.GetHistoryAsync(roomName).GetAwaiter().GetResult())));
        try
        {
            return room.Value;
        }
        catch
        {
            // A Lazy keeps a failed load's exception, so forget it and let the next subscriber try again.
            _rooms.TryRemove(new KeyValuePair<string, Lazy<ChatRoom>>(name, room));
            throw;
        }
    }
}

// Chat/IChatPublisher.cs
public interface IChatPublisher
{
    Task Publish(ChatMessageEnvelope envelope);
}

// Chat/ChatRoomPage.cs
public record ChatMessageId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static ChatMessageId New() => new(Guid.NewGuid());
}

// The wire format on the broker: the message plus the room it belongs to.
public record ChatMessageEnvelope(string RoomName, ChatMessageId Id, string User, DateTimeOffset SentAt, string Message);

[ReadModel]
public record ChatMessage(ChatMessageId Id, string User, DateTimeOffset SentAt, string Message)
{
    public static ISubject<IEnumerable<ChatMessage>> ForRoom(string roomName, ChatService chatService)
    {
        var room = chatService.GetChatRoom(roomName);
        // A relay per subscriber, seeded with the room's current history. It follows the room only
        // while Arc is subscribed: Observable.Using ends the room subscription when the client leaves.
        var relay = new BehaviorSubject<IEnumerable<ChatMessage>>(room.Messages.Value);
        return Subject.Create<IEnumerable<ChatMessage>>(relay, Observable.Using(() => room.Messages.Subscribe(relay), _ => relay));
    }
}

[Command]
public record SendMessage(string RoomName, string User, string Message)
{
    // Publishes to the broker; the room updates when the message comes back from it.
    public Task Handle(IChatPublisher publisher) =>
        publisher.Publish(new(RoomName, ChatMessageId.New(), User, DateTimeOffset.UtcNow, Message));
}
```
