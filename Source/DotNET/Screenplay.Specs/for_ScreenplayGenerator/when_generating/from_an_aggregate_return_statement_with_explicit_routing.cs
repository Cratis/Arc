// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_aggregate_return_statement_with_explicit_routing : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, AggregateRoutingSources.With("""
        public Task Handle(Author author, Cratis.Chronicle.EventSequences.IEventLog log) => log.Append(OtherId, author.Build(Name));
        """).Replace("public Task Register(string name) => Apply(new AuthorRegistered(name));", "public AuthorRegistered Build(string name) { return new(name); }", StringComparison.Ordinal)));

    [Fact] void should_not_inline_the_event() => Result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_not_state_an_identifier() => Result.Source.ShouldNotContain("identifier");
    [Fact] void should_not_state_a_destination() => Result.Source.ShouldNotContain("for id");
    [Fact] void should_report_the_destination_once() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(1);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
