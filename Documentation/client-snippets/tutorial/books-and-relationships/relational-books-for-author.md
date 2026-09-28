```csharp
[ReadModel]
public record Book(BookId Id, AuthorId AuthorId, BookTitle Title)
{
    public static ISubject<IEnumerable<Book>> BooksForAuthor(AuthorId authorId, LibraryDbContext db) =>
        db.Books.Observe(b => b.AuthorId == authorId);
}
```
