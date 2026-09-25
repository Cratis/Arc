```csharp
[ReadModel]
public record Author(AuthorId Id, AuthorName Name)
{
    [Roles(nameof(LibraryRole.Librarian))]
    public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> authors) =>
        authors.Observe();
}
```
