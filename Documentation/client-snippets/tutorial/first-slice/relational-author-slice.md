```csharp
[Command]
public record RegisterAuthor(AuthorId Id, AuthorName Name)
{
    public async Task Handle(LibraryDbContext db)
    {
        db.Authors.Add(new Author(Id, Name));
        await db.SaveChangesAsync();
    }
}

[ReadModel]
public record Author(AuthorId Id, AuthorName Name)
{
    public static ISubject<IEnumerable<Author>> AllAuthors(LibraryDbContext db) =>
        db.Authors.Observe();
}
```
