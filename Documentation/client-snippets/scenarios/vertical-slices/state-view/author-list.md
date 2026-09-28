```csharp
using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Driver;

[ReadModel]
[FromEvent<AuthorRegistered>]
public record Author(AuthorId Id, AuthorName FirstName, AuthorName LastName)
{
    public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> collection) =>
        collection.Observe();
}
```
