```csharp
[Command]
public record RegisterAuthor(AuthorId Id, AuthorName FullName)   // was Name
{
    public Task Handle(IMongoCollection<Author> authors) =>
        authors.InsertOneAsync(new Author(Id, FullName));
}
```
