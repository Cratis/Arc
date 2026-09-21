```csharp
[Command]
public record AddBook(AuthorId AuthorId, BookId BookId, BookTitle Title)
{
    public Task Handle(IMongoCollection<Book> books) =>
        books.InsertOneAsync(new Book(BookId, AuthorId, Title));
}
```
