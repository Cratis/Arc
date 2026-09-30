```csharp
using (var scope = app.Services.CreateScope())
{
    var authors = scope.ServiceProvider.GetRequiredService<IMongoCollection<Author>>();
    await authors.Indexes.CreateOneAsync(new CreateIndexModel<Author>(
        Builders<Author>.IndexKeys.Ascending(author => author.Name),
        new CreateIndexOptions { Unique = true, Name = "unique_author_name" }));
}
```
