```csharp
[Roles("Librarian")]                       // only a Librarian may register an author
[Command]
public record RegisterAuthor(AuthorId Id, AuthorName Name)
{
    public Task Handle(IMongoCollection<Author> authors) =>
        authors.InsertOneAsync(new Author(Id, Name));
}

[ReadModel]
public record Author(AuthorId Id, AuthorName Name)
{
    [AllowAnonymous]                        // the public catalog is open to everyone
    public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> c) => c.Observe();
}
```
