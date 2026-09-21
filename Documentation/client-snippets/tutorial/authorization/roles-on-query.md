```csharp
[ReadModel]
public record Author(AuthorId Id, AuthorName Name)
{
    [Roles("Librarian")]
    public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> authors) =>
        authors.Observe();
}
```
