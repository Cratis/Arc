// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// The three dimensions a command's appends can be checked for concurrent writers within are declared with the same
/// attributes that name them, and only take part in the scope when the second argument says so. An attribute naming
/// a dimension without opting in is metadata about where events go, not a scope, and stating it as one would
/// describe a stricter application than the source does.
/// </summary>
public class a_command_declaring_a_concurrency_scope : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Inventory.Adding;

        [EventType]
        public record BookAdded(string Title);

        [Command]
        [EventStreamType("Inventory", true)]
        [EventSourceType("Book")]
        public record AddBook(string Title)
        {
            public BookAdded Handle() => new(Title);
        }

        [Command]
        public record AddBookWithoutAScope(string Title)
        {
            public BookAdded Handle() => new(Title);
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    Model.CommandModel Command(string name) => _analysis.Slice().Commands.First(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_recover_the_scope() => Command("AddBook").Concurrency.ShouldNotBeNull();
    [Fact] void should_narrow_by_the_dimension_that_opted_in() => Command("AddBook").Concurrency!.StreamType.ShouldEqual("Inventory");
    [Fact] void should_leave_out_the_dimension_that_did_not() => Command("AddBook").Concurrency!.SourceType.ShouldBeNull();
    [Fact] void should_declare_no_scope_when_the_command_declares_none() => Command("AddBookWithoutAScope").Concurrency.ShouldBeNull();
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
