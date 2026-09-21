```csharp
public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> authors) =>
    authors.Observe();
```
