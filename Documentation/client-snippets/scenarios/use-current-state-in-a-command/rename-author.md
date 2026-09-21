```csharp
using System.ComponentModel.DataAnnotations;

[Command]
public record RenameAuthor([property: Key] AuthorId Id, AuthorName NewName)
{
    public Task Handle(Author author, IMongoCollection<Author> authors) =>
        authors.ReplaceOneAsync(existing => existing.Id == author.Id, author with { Name = NewName });
}
```
