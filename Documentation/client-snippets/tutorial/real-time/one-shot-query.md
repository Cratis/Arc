```csharp
public static IEnumerable<Author> AllAuthors(IMongoCollection<Author> authors) =>
    authors.Find(_ => true).ToList();
```
