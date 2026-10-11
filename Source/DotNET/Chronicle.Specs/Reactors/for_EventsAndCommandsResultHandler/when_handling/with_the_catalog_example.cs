// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_handling;

public class with_the_catalog_example : given.a_mixed_result_handler
{
    BookId _bookId;
    BookTitle _title;

    void Establish()
    {
        _bookId = BookId.New();
        _title = new("Eventful books");
        _store.EventTypes.HasFor(typeof(BookIndexingRequested)).Returns(true);
        _context = _context with { EventContext = EventContext.Empty with { EventSourceId = _bookId } };
    }

    async Task Because()
    {
        var effects = new CatalogIndexer().BookAdded(new BookAddedToCatalog(_title), _context.EventContext);
        _result = await _dispatcher.Handle(_context, _store, effects);
    }

    [Fact] void should_record_the_indexing_request_before_executing_the_command() => _order.SequenceEqual(["append", "commands"]).ShouldBeTrue();
    [Fact] void should_record_the_request_for_the_book() => _appended.Single().ShouldEqual(new EventForEventSourceId(_bookId, new BookIndexingRequested(_title)));
    [Fact] void should_execute_the_indexing_command() => _executed.ShouldContainOnly(new CreateSearchIndex(_bookId, _title));
    [Fact] void should_discover_the_example_reactor() => typeof(CatalogIndexer).GetMethod(nameof(CatalogIndexer.BookAdded))!.IsEventHandlerMethod([typeof(BookAddedToCatalog)], _dispatcher).ShouldBeTrue();

    public record BookId(Guid Value) : EventSourceId<Guid>(Value)
    {
        public static BookId New() => new(Guid.NewGuid());
        public static implicit operator BookId(Guid value) => new(value);
    }

    public record BookTitle(string Value) : ConceptAs<string>(Value);

    [Command]
    public record CreateSearchIndex(BookId BookId, BookTitle Title)
    {
        public Task Handle(ISearchIndex index) => index.Upsert(BookId, Title);
    }

    public interface ISearchIndex
    {
        Task Upsert(BookId bookId, BookTitle title);
    }

    [EventType]
    public record BookAddedToCatalog(BookTitle Title);

    [EventType]
    public record BookIndexingRequested(BookTitle Title);

    public class CatalogIndexer : IReactor
    {
        [OnceOnly]
        public IEnumerable<object> BookAdded(BookAddedToCatalog @event, EventContext context)
        {
            BookId bookId = Guid.Parse(context.EventSourceId.Value);
            return
            [
                new EventForEventSourceId(context.EventSourceId, new BookIndexingRequested(@event.Title)),
                new CreateSearchIndex(bookId, @event.Title)
            ];
        }
    }
}
