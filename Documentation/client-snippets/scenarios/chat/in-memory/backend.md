```csharp
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Concepts;

namespace MyApp.Chat;

// Chat/ChatRoom.cs
public class ChatRoom
{
    readonly object _lock = new();
    readonly List<ChatMessage> _history = [];

    // Holds the room's full history and hands it to every new subscriber.
    public BehaviorSubject<IEnumerable<ChatMessage>> Messages { get; } = new([]);

    public void Send(string user, string message)
    {
        lock (_lock)
        {
            _history.Add(new ChatMessage(ChatMessageId.New(), user, DateTimeOffset.UtcNow, message));
            Messages.OnNext([.. _history]);
        }
    }
}

// Register as a singleton: builder.Services.AddSingleton<ChatService>();
public class ChatService
{
    readonly ConcurrentDictionary<string, ChatRoom> _rooms = new();

    public ChatRoom GetChatRoom(string name) => _rooms.GetOrAdd(name, _ => new ChatRoom());
}

// Chat/ChatRoomPage.cs
public record ChatMessageId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static ChatMessageId New() => new(Guid.NewGuid());
}

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
    public void Handle(ChatService chatService) => chatService.GetChatRoom(RoomName).Send(User, Message);
}
```
