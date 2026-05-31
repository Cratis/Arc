---
title: Query data across slices
description: Serve a screen that needs data spanning more than one feature — by folding several events into one read model, or filtering a query by a relationship.
---

**Goal:** a screen needs data that doesn't live in a single slice — authors *with* their books, an order *with* its line items, anything where one thing owns a list of another.

## Fold it at write time, not read time

The instinct from relational databases is to store a foreign key and join at read time. Event sourcing flips that: a projection folds the relevant events into one **purpose-built read model**, so the query is a plain read with no join. You build the shape the screen needs.

## Do it

**For an owns-a-list relationship, use `[ChildrenFrom<…>]`.** Declare the child shape, then declare the parent with a children property that tells the projection how to attach them:

```csharp
[FromEvent<BookAddedToCatalog>]
public record Book([Key] BookId Id, BookTitle Title);

[ReadModel]
[FromEvent<AuthorRegistered>]
public record AuthorWithBooks(
    [Key] AuthorId Id,
    AuthorName Name,
    [ChildrenFrom<BookAddedToCatalog>(parentKey: nameof(BookAddedToCatalog.AuthorId), identifiedBy: nameof(Book.Id))]
    IEnumerable<Book> Books)
{
    public static ISubject<IEnumerable<AuthorWithBooks>> AllAuthorsWithBooks(IMongoCollection<AuthorWithBooks> collection) =>
        collection.Observe();
}
```

Each `AuthorWithBooks` arrives already carrying its catalog — one query, no stitching.

**For "just the rows that match", filter the query.** `Observe` takes a predicate, and a query method can take parameters that bind from the request:

```csharp
public static ISubject<IEnumerable<Book>> BooksForAuthor(AuthorId author, IMongoCollection<Book> collection) =>
    collection.Observe(book => book.AuthorId == author);
```

Both forms stay live: because they return `Observe()`, the screen updates as new events land.

## See also

- [Queries](/arc/backend/queries/) — observable and non-observable query methods, parameters, and filtering.
- [Relate your slices](/arc/tutorial/books-and-relationships/) — the children projection built step by step.
- [Run a command from React](./run-a-command-from-react.md) — consuming the query proxy.
