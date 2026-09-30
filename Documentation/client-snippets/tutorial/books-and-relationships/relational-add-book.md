```csharp
[Command]
public record AddBook(AuthorId AuthorId, BookId BookId, BookTitle Title)
{
    public async Task Handle(LibraryDbContext db)
    {
        db.Books.Add(new Book(BookId, AuthorId, Title));
        await db.SaveChangesAsync();
    }
}
```
