// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_reading_read_models_keyed_other_than_by_event_source
{
    [Fact]
    public async Task read_model_keyed_by_an_event_property_is_served_for_the_event_source_whose_events_produced_it()
    {
        await using var scenario = new CommandScenario<ReadNotesByAuthor>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.Given.ForEventSource(source).Events(new NoteFiled(Guid.NewGuid(), "filed"));
        (await scenario.Execute(new ReadNotesByAuthor(source))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReadNotesByAuthor, NotesObserved>(source, _ => _.Summary == "filed");
    }

    [Fact]
    public async Task read_model_keyed_by_an_event_property_does_not_see_the_events_of_another_source()
    {
        await using var scenario = new CommandScenario<ReadNotesByAuthor>().UseDecisionReads();
        var author = EventSourceId.New();
        var otherSource = EventSourceId.New();
        scenario.Given.ForEventSource(otherSource).Events(new NoteFiled(Guid.Parse(author.Value), "filed"));
        (await scenario.Execute(new ReadNotesByAuthor(author))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReadNotesByAuthor, NotesObserved>(author, _ => _.Summary == "absent");
    }

    [Fact]
    public async Task read_model_with_children_seeded_for_another_source_materializes_without_them()
    {
        await using var scenario = new CommandScenario<ReadOrder>().UseDecisionReads();
        var order = EventSourceId.New();
        var itemSource = EventSourceId.New();
        scenario.Given.ForEventSource(order).Events(new OrderPlaced("order"));
        scenario.Given.ForEventSource(itemSource).Events(new ItemAdded(Guid.Parse(order.Value), Guid.NewGuid()));
        (await scenario.Execute(new ReadOrder(order))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<ReadOrder, NotesObserved>(order, _ => _.Summary == "items:0");
    }

    [Fact]
    public async Task read_model_keyed_by_an_event_property_fails_when_the_events_of_the_source_resolve_to_several_instances()
    {
        await using var scenario = new CommandScenario<ReadNotesByAuthor>().UseDecisionReads();
        var source = EventSourceId.New();
        scenario.Given.ForEventSource(source).Events(new NoteFiled(Guid.NewGuid(), "first"), new NoteFiled(Guid.NewGuid(), "second"));
        var result = await scenario.Execute(new ReadNotesByAuthor(source));
        result.IsSuccess.ShouldBeFalse();
    }

    [Command]
    [ProtectedDecision]
    public record ReadNotesByAuthor(EventSourceId EventSourceId)
    {
        public NotesObserved Handle(DecisionRead<Ledger> ledger, NotesByAuthor? notes) => new(notes?.Note ?? "absent");
    }

    [Command]
    [ProtectedDecision]
    public record ReadOrder(EventSourceId EventSourceId)
    {
        public NotesObserved Handle(DecisionRead<Ledger> ledger, Order? order) => new(order is null ? "absent" : $"items:{order.Items.Count()}");
    }

    [FromEvent<LedgerOpened>]
    public record Ledger([property: Key] Guid Id);

    [FromEvent<NoteFiled>(key: nameof(NoteFiled.AuthorId))]
    public record NotesByAuthor([property: Key] Guid Id, string Note);

    [FromEvent<OrderPlaced>]
    public record Order(
        [property: Key] Guid Id,
        string Name,
        [ChildrenFrom<ItemAdded>(key: nameof(ItemAdded.ItemId), parentKey: nameof(ItemAdded.OrderId))] IEnumerable<OrderItem> Items);

    public record OrderItem([property: Key] Guid Id);

    [EventType("0b7d7a3e-5b0c-4e0e-9d52-6a1c1d3b7a01")]
    public record LedgerOpened;

    [EventType("0b7d7a3e-5b0c-4e0e-9d52-6a1c1d3b7a02")]
    public record NoteFiled(Guid AuthorId, string Note);

    [EventType("0b7d7a3e-5b0c-4e0e-9d52-6a1c1d3b7a03")]
    public record OrderPlaced(string Name);

    [EventType("0b7d7a3e-5b0c-4e0e-9d52-6a1c1d3b7a04")]
    public record ItemAdded(Guid OrderId, Guid ItemId);

    [EventType("0b7d7a3e-5b0c-4e0e-9d52-6a1c1d3b7a05")]
    public record NotesObserved(string Summary);
}
