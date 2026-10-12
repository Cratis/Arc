// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_unrepresentable_metadata : a_filtered_application
{
    const string Handler = "public Task<SendWelcome> Welcome(AuthorRegistered @event) => Task.FromResult(new SendWelcome(@event.Name, @event.Country));";
    const string Definition = "[Cratis.Chronicle.EventSources.EventSource(\"Author\")] [Cratis.Chronicle.EventSources.EventStream(\"Onboarding\")] public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;";

    [Theory]
    [InlineData("", "[EventSourceType(\"Unknown\")]")]
    [InlineData(Definition, "[EventSourceType(\"*\")]")]
    [InlineData(Definition, "[EventStreamType(\"*\")]")]
    [InlineData(Definition, "[EventStreamType(\"Onboarding\")]")]
    [InlineData(Definition, "[Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>(\"Missing\")]")]
    [InlineData(Definition, "[Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>(\"All\")]")]
    [InlineData("[Cratis.Chronicle.EventSources.EventSource(\"Default\")] public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;", "[EventSourceType(\"Default\")]")]
    [InlineData("[Cratis.Chronicle.EventSources.EventSource(\"Stored-name\")] public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;", "[EventSourceType(\"Stored-name\")]")]
    [InlineData("[Cratis.Chronicle.EventSources.EventSource(\"Author\\n\")] public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;", "[EventSourceType(\"Author\\n\")]")]
    [InlineData("[Cratis.Chronicle.EventSources.EventSource(\"Author\")] [Cratis.Chronicle.EventSources.EventStream(\"Unsafe-name\")] public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;", "[EventSourceType(\"Author\")]")]
    [InlineData("[Cratis.Chronicle.EventSources.EventSource(\"Author\")] [Cratis.Chronicle.EventSources.EventStream(\"Onboarding\")] [Cratis.Chronicle.EventSources.EventStream(\"Onboarding\")] public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;", "[EventSourceType(\"Author\")]")]
    public void should_refuse_filters_without_a_truthful_declaration(string declarations, string attributes)
    {
        GenerateFilteredWith($$"""{{declarations}} {{attributes}} [ExecuteCommandsAsSystem("Auditor")] public class Welcomer : IReactor { {{Handler}} }""");

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldContain("runs as system role \"Auditor\"");
        Result.Source.ShouldNotContain("invokes SendWelcome");
        Result.Source.ShouldNotContain("from ");
        Result.Source.ShouldNotContain("eventsource ");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable).Message.ShouldContain("no unfiltered executable effect was substituted");
        AssertDocument();
    }

    [Fact]
    public void should_refuse_colliding_runtime_definition_names()
    {
        GenerateFilteredWith($$"""
            {{Definition}}
            [Cratis.Chronicle.EventSources.EventSource("Author")] public class OtherEventSource : Cratis.Chronicle.EventSources.IEventSource;
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>("Onboarding")]
            public class Welcomer : IReactor { {{Handler}} }
            """);

        Result.Source.ShouldNotContain("from Author");
        Result.Source.ShouldNotContain("invokes SendWelcome");
        Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable);
        AssertDocument();
    }

    [Theory]
    [InlineData("[EventStreamId(\"Elsewhere\")]", "", "")]
    [InlineData("[EventStreamId(null)]", "", "")]
    [InlineData("", ", ICanProvideEventSourceId", "public EventSourceId GetEventSourceId() => \"elsewhere\";")]
    [InlineData("", ", ICanProvideEventStreamId", "public EventStreamId GetEventStreamId() => \"elsewhere\";")]
    public void should_keep_provider_and_routed_event_effects_as_whole_source_triggers(string attributes, string provider = "", string implementation = "")
    {
        GenerateFilteredWith($$"""
            {{Definition}}
            {{attributes}}
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>("Onboarding")]
            public class Welcomer : IReactor{{provider}}
            {
                {{implementation}}
                public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, "Welcome");
            }
            """);

        Result.Source.ShouldContain("from Author.Onboarding");
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("produces AuthorWelcomed");
        Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.UnrepresentableReactionAppendRoute);
        AssertDocument();
    }
}
