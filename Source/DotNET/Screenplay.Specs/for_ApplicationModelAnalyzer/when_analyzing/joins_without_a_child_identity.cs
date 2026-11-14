// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class joins_without_a_child_identity : Specification
{
    const string Source = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;

        namespace Library.Codes.Listing;

        [EventType]
        public record UnitObserved(string Unit);

        [EventType]
        public record MonthOpened(string Unit);

        public record Entry(Guid CodeId, string Unit);

        [ReadModel]
        public record Codes(Guid Id, string Unit, Entry Detail, IEnumerable<Entry> Entries);

        public class CodesProjection : IProjectionFor<Codes>
        {
            public void Define(IProjectionBuilderFor<Codes> builder) => builder
                .Join<UnitObserved>(j => j.On(m => m.Id).Set(m => m.Unit).To(e => e.Unit))
                .Join<MonthOpened>()
                .Children(m => m.Entries, c => c.Join<UnitObserved>())
                .Nested(m => m.Detail, n => n.Join<MonthOpened>());
        }
        """;

    ProjectionScopeModel _scope;

    void Because() => _scope = Analyzed.Source(Source).Slice().Projections.Single().Scope;

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_keep_the_explicit_root_key() => _scope.Joins.Single(join => join.EventType == "UnitObserved").On.ShouldEqual("Id");
    [Fact] void should_keep_the_explicit_root_property() => _scope.Joins.Single(join => join.EventType == "UnitObserved").Property.ShouldEqual("Unit");
    [Fact] void should_not_invent_a_root_key() => _scope.Joins.Single(join => join.EventType == "MonthOpened").On.ShouldEqual(string.Empty);
    [Fact] void should_not_invent_a_root_property() => _scope.Joins.Single(join => join.EventType == "MonthOpened").Property.ShouldEqual(string.Empty);
    [Fact] void should_not_invent_a_child_key() => _scope.Children.Single().Scope.Joins.Single().On.ShouldEqual(string.Empty);
    [Fact] void should_not_invent_a_child_property() => _scope.Children.Single().Scope.Joins.Single().Property.ShouldEqual(string.Empty);
    [Fact] void should_not_invent_a_nested_key() => _scope.Nested.Single().Scope.Joins.Single().On.ShouldEqual(string.Empty);
    [Fact] void should_not_invent_a_nested_property() => _scope.Nested.Single().Scope.Joins.Single().Property.ShouldEqual(string.Empty);
}
