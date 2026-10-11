```csharp
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;

[ReadModel]
public record Author(Guid Id, string Name)
{
    public static IEnumerable<Author> AllAuthors(IMongoCollection<Author> authors) =>
        authors.Find(_ => true).ToList();
}
```
