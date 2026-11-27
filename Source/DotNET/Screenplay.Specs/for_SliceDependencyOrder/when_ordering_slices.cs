// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Slices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.for_SliceDependencyOrder;

public class when_ordering_slices : Specification
{
    static readonly SourceLocation _location = SourceLocation.Start;

    [Fact]
    void should_write_the_declaring_slice_before_the_consuming_slice() =>
        Names(SliceDependencyOrder.Apply([Consumer("Alpha", "Placed"), Declarer("Zulu", "Placed")])).ShouldEqual("Zulu,Alpha");

    [Fact]
    void should_keep_ordinal_order_when_nothing_depends() =>
        Names(SliceDependencyOrder.Apply([Declarer("Zulu", "A"), Declarer("Alpha", "B")])).ShouldEqual("Alpha,Zulu");

    [Fact]
    void should_keep_every_slice_of_a_cycle_in_ordinal_order() =>
        Names(SliceDependencyOrder.Apply([Both("Beta", "B", "A"), Both("Alpha", "A", "B")])).ShouldEqual("Alpha,Beta");

    [Fact]
    void should_keep_a_downstream_consumer_after_the_cycle_it_depends_on() =>
        Names(SliceDependencyOrder.Apply([Slice("Alpha", [], ["C", "D"]), Both("Charlie", "C", "D"), Both("Delta", "D", "C")])).ShouldEqual("Charlie,Delta,Alpha");

    [Fact]
    void should_add_no_edge_when_two_siblings_declare_the_event() =>
        Names(SliceDependencyOrder.Apply([Consumer("Alpha", "Placed"), Declarer("Yankee", "Placed"), Declarer("Zulu", "Placed")])).ShouldEqual("Alpha,Yankee,Zulu");

    [Fact]
    void should_add_no_edge_for_a_slice_consuming_its_own_event() =>
        Names(SliceDependencyOrder.Apply([Both("Zulu", "Placed", "Placed"), Declarer("Alpha", "Other")])).ShouldEqual("Alpha,Zulu");

    static string Names(IEnumerable<SliceSyntax> slices) => string.Join(',', slices.Select(_ => _.Name));

    static SliceSyntax Declarer(string name, string @event) => Slice(name, [@event], []);

    static SliceSyntax Consumer(string name, string @event) => Slice(name, [], [@event]);

    static SliceSyntax Both(string name, string declared, string consumed) => Slice(name, [declared], [consumed]);

    static SliceSyntax Slice(string name, string[] declared, string[] consumed) =>
        new(
            SliceType.StateView,
            name,
            declared.Select(_ => new EventSyntax(_, [], _location)),
            [],
            [],
            consumed.Select(_ => new ProjectionSyntax(name + "Projection", null, null, default, null, [new FromSyntax([new EventSpecSyntax(_, null, _location)], null, null, [], _location)], _location)),
            [],
            [],
            [],
            [],
            [],
            _location);
}
