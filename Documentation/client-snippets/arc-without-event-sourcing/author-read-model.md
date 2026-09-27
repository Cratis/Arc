```csharp
[ReadModel]
public record Author(AuthorId Id, AuthorName Name)
{
    // This static method is the query — served over HTTP, and live.
    public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> authors) =>
        authors.Observe();
}
```
