// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_stage_rendered_filter_reproduction : a_generated_document
{
    const string Definition = """
        namespace Filters.EventSources;
        [global::Cratis.Chronicle.EventSources.EventSourceAttribute("stored-account")]
        [global::Cratis.Chronicle.EventSources.EventStreamAttribute(AccountEventSource.Archive)]
        [global::Cratis.Chronicle.EventSources.EventStreamAttribute(AccountEventSource.Notes)]
        public class AccountEventSource : global::Cratis.Chronicle.EventSources.IEventSource
        {
            public const string Archive = "stored-archive";
            public const string Notes = "stored-notes";
        }
        """;
    const string Reactor = """
        namespace Filters.Work.Processing.FollowUp;
        [global::Cratis.Chronicle.Events.EventTypeAttribute]
        public record Started(string Id, decimal Amount);
        [global::Cratis.Chronicle.Events.EventTypeAttribute]
        public record Followed(decimal Amount);
        [global::Cratis.Chronicle.Reactors.ReactorAttribute("Worker")]
        [global::Cratis.Chronicle.Reactors.OnceOnlyAttribute]
        [global::Cratis.Chronicle.EventSources.FromEventSourceAttribute<global::Filters.EventSources.AccountEventSource>(global::Filters.EventSources.AccountEventSource.Notes)]
        public class Worker : global::Cratis.Chronicle.Reactors.IReactor
        {
            public global::System.Collections.Generic.IEnumerable<object> Started(global::Filters.Work.Processing.FollowUp.Started @event, global::Cratis.Chronicle.Events.EventContext context)
            {
                var effects = new global::System.Collections.Generic.List<object>();
                effects.Add(new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(@event.Id, new global::Filters.Work.Processing.FollowUp.Followed(@event.Amount)));
                return effects;
            }
        }
        """;

    void Establish() => Generate(
        ("Filters/EventSources/AccountEventSource.cs", Definition),
        ("Filters/Work/Processing/FollowUp/Worker.cs", Reactor));

    [Fact] void should_recover_the_source_and_stream_filter() => Result.Source.ShouldContain("from Account.Notes");
    [Fact] void should_recover_the_production() => Result.Source.ShouldContain("produces Followed");
    [Fact] void should_preserve_the_explicit_destination() => Result.Source.ShouldContain("for id");
    [Fact] void should_preserve_the_stored_source_name() => Result.Source.ShouldContain("id \"stored-account\"");
    [Fact] void should_preserve_the_stored_stream_name() => Result.Source.ShouldContain("id \"stored-notes\"");
    [Fact] void should_recover_the_whole_reaction() => Result.Source.ShouldNotContain("file Work/Processing/FollowUp/Worker.cs");
    [Fact] void should_not_report_an_unrepresentable_filter() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable).ShouldBeEmpty();
    [Fact] void should_compile_and_bind_the_recovered_document() => AssertDocument();
}
