```csharp
using Cratis.Arc.Commands.ModelBound;
using MongoDB.Driver;

[Command]
public record RegisterAuthor(Guid Id, string Name)
{
    public Task Handle(IMongoCollection<Author> authors) =>
        authors.InsertOneAsync(new Author(Id, Name));
}
```
