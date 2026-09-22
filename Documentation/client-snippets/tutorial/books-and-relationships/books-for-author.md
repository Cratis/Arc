```csharp
[ReadModel]
public record Book(BookId Id, AuthorId AuthorId, BookTitle Title)
{
    public static ISubject<IEnumerable<Book>> BooksForAuthor(AuthorId authorId, IMongoCollection<Book> books) =>
        books.Observe(b => b.AuthorId == authorId);
}
```
