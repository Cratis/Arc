---
title: Query data across slices
description: Serve a screen that needs data spanning more than one feature — by filtering live queries around a relationship.
---

**Goal:** a screen needs data that doesn't live in a single slice — authors *with* their books, an order *with* its line items, anything where one thing owns a list of another.

## Keep the relationship explicit

In a database-backed Arc slice, model the relationship the same way you would in the domain: the child carries the parent's id, and the query filters by it. Arc's part is making that query a typed, live endpoint and generating the React proxy that calls it.

## Do it

**For an owns-a-list relationship, keep the child keyed to its parent.** A book belongs to an author, so the `Book` read model carries `AuthorId`:

```csharp
[ReadModel]
public record Book([property: Key] BookId Id, AuthorId AuthorId, BookTitle Title)
{
    public static ISubject<IEnumerable<Book>> BooksForAuthor([Key] AuthorId authorId, IMongoCollection<Book> books) =>
        books.Observe(book => book.AuthorId == authorId);
}
```

The generated proxy takes the same parameter:

```tsx
const [books] = BooksForAuthor.use(authorId);
```

Because the query returns `Observe(...)`, it stays live. Add a book for that author and the row updates without polling.

For a list-with-details screen, let the author list call `AllAuthors.use()` and each row call `BooksForAuthor.use(author.id)`. That keeps each slice small: the author slice owns authors, the book slice owns books, and the screen composes their generated proxies.

## See also

- [Queries](/arc/backend/queries/) — observable and non-observable query methods, parameters, and filtering.
- [Relate your slices](/arc/tutorial/books-and-relationships/) — this relationship built step by step.
- [Run a command from React](./run-a-command-from-react.md) — consuming the query proxy.
