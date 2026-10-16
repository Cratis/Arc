```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Concepts;
using FluentValidation;

public record Isbn(string Value) : EventSourceId(Value);

public record MemberId(Guid Value) : ConceptAs<Guid>(Value);

[EventType]
public record CopyAdded();

[EventType]
public record BookBorrowed(MemberId Member);

[EventType]
public record BookReturned(MemberId Member);

[ReadModel]
public record BookStock(
    Isbn Id,
    [Increment<CopyAdded>, Decrement<BookBorrowed>, Increment<BookReturned>] int Available);

[Command, ReadProtection(ReadProtectionMode.On)]
public record BorrowBook(Isbn Isbn, MemberId Member)
{
    public BookBorrowed Handle() => new(Member);
}

public class BorrowBookValidator : CommandValidator<BorrowBook>
{
    public BorrowBookValidator(BookStock? stock) =>
        RuleFor(command => command.Isbn)
            .Must(_ => stock is { Available: > 0 })
            .WithMessage("No copies of this book are available.");
}
```
