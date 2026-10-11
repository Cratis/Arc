// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_observation_metadata : a_filtered_application
{
    const string Definition = "[Cratis.Chronicle.EventSources.EventSource(\"Author\")] [Cratis.Chronicle.EventSources.EventStream(\"Onboarding\")] public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;";
    const string ReturnedEvent = "public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, \"Welcome\");";
    const string ReturnedCommand = "public Task<SendWelcome> Welcome(AuthorRegistered @event) => Task.FromResult(new SendWelcome(@event.Name, @event.Country));";

    [Theory]
    [InlineData("public void Welcome(AuthorRegistered @event) { Console.WriteLine(@event.Name); }", false)]
    [InlineData(ReturnedCommand, true)]
    public void should_preserve_identity_and_typed_observation_independently_of_body_recovery(string handler, bool declarative)
    {
        GenerateFilteredWith($$"""
            {{Definition}}
            [ExecuteCommandsAsSystem("Librarian")]
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>("Onboarding")]
            public class Welcomer : IReactor { {{handler}} }
            """);

        Result.Source.ShouldContain("eventsource Author\n  stream Onboarding");
        Result.Source.ShouldContain("from Author.Onboarding");
        Result.Source.ShouldContain("runs as system role \"Librarian\"");
        Result.Source.Contains("invokes SendWelcome", StringComparison.Ordinal).ShouldEqual(declarative);
        Result.Source.Contains("file Authors/Welcoming/Welcoming.cs", StringComparison.Ordinal).ShouldEqual(!declarative);
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable).ShouldBeEmpty();
        AssertDocument();
    }

    [Theory]
    [InlineData(ReturnedEvent, false)]
    [InlineData(ReturnedCommand, true)]
    public void should_resolve_typed_observation_before_raw_filters_but_keep_raw_append_routing(string handler, bool invokes)
    {
        GenerateFilteredWith($$"""
            {{Definition}}
            [EventSourceType("Other")]
            [EventStreamType("Elsewhere")]
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>("Onboarding")]
            public class Welcomer : IReactor { {{handler}} }
            """);

        Result.Source.ShouldContain("from Author.Onboarding");
        Result.Source.ShouldNotContain("from Other");
        Result.Source.ShouldNotContain("produces AuthorWelcomed");
        Result.Source.Contains("invokes SendWelcome", StringComparison.Ordinal).ShouldEqual(invokes);
        Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.UnrepresentableReactionAppendRoute);
        AssertDocument();
    }

    [Theory]
    [InlineData("[EventSourceType(\"Author\")]", "from Author\n")]
    [InlineData("[EventSourceType(\"Author\")] [EventStreamType(\"All\")]", "from Author\n")]
    [InlineData("[EventSourceType(\"Author\")] [EventStreamType(\"Onboarding\")]", "from Author.Onboarding")]
    public void should_resolve_raw_filters_from_real_observer_only_definitions(string attributes, string expected)
    {
        GenerateFilteredWith($$"""{{Definition}} {{attributes}} public class Welcomer : IReactor { {{ReturnedCommand}} }""");

        Result.Source.ShouldContain(expected);
        Result.Source.ShouldContain("eventsource Author");
        new ScreenplayCompiler().Compile(Result.Source).Value!.EventSources.Single().Identifier.ShouldBeNull();
        Result.Source.ShouldContain("invokes SendWelcome");
        AssertDocument();
    }

    [Fact]
    public void should_not_use_typed_observation_as_an_append_route()
    {
        GenerateFilteredWith($$"""{{Definition}} [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>("Onboarding")] public class Welcomer : IReactor { {{ReturnedEvent}} }""");

        Result.Source.ShouldContain("from Author.Onboarding");
        Result.Source.ShouldContain("produces AuthorWelcomed");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableReactionAppendRoute).ShouldBeEmpty();
        AssertDocument();
    }

    [Fact]
    public void should_observe_all_streams_for_the_registered_all_sentinel()
    {
        GenerateFilteredWith($$"""
            [Cratis.Chronicle.EventSources.EventSource] [Cratis.Chronicle.EventSources.EventStream("All")]
            public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>("All")]
            public class Welcomer : IReactor { {{ReturnedEvent}} }
            """);

        Result.Source.ShouldContain("from Author\n");
        Result.Source.ShouldNotContain("from Author.All");
        Result.Source.ShouldContain("produces AuthorWelcomed");
        AssertDocument();
    }

    [Fact]
    public void should_preserve_valid_stored_names_without_name_conversion()
    {
        GenerateFilteredWith($$"""
            [Cratis.Chronicle.EventSources.EventSource("authors_exact")] [Cratis.Chronicle.EventSources.EventStream("onboarding_exact")]
            public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>("onboarding_exact")]
            public class Welcomer : IReactor { {{ReturnedEvent}} }
            """);

        Result.Source.ShouldContain("eventsource authors_exact");
        Result.Source.ShouldContain("from authors_exact.onboarding_exact");
        AssertDocument();
    }

    [Fact]
    public void should_keep_mixed_return_bodies_opaque_without_losing_metadata()
    {
        GenerateFilteredWith($$"""
            {{Definition}}
            [ExecuteCommandsAsSystem("Librarian")]
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>("Onboarding")]
            public class Welcomer : IReactor
            {
                public IEnumerable<object> Welcome(AuthorRegistered @event) => [new AuthorWelcomed(@event.Name, "Welcome"), new SendWelcome(@event.Name, @event.Country)];
            }
            """);

        Result.Source.ShouldContain("from Author.Onboarding");
        Result.Source.ShouldContain("runs as system role \"Librarian\"");
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("produces AuthorWelcomed");
        Result.Source.ShouldNotContain("invokes SendWelcome");
        AssertDocument();
    }

    [Fact]
    public void should_resolve_nearest_raw_source_and_stream_independently()
    {
        GenerateFilteredWith($$"""
            {{Definition}}
            [EventSourceType("Other")] [EventStreamType("Onboarding")] public abstract class Parent;
            [EventSourceType("Author")] public class Welcomer : Parent, IReactor { {{ReturnedCommand}} }
            """);

        Result.Source.ShouldContain("from Author.Onboarding");
        AssertDocument();
    }

    [Fact]
    public void should_not_inherit_the_typed_observation_attribute()
    {
        GenerateFilteredWith($$"""
            {{Definition}}
            [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>("Onboarding")] public abstract class Parent;
            public class Welcomer : Parent, IReactor { {{ReturnedEvent}} }
            """);

        Result.Source.ShouldNotContain("from Author");
        Result.Source.ShouldContain("produces AuthorWelcomed");
        AssertDocument();
    }

    [Theory]
    [InlineData("[EventSourceType(\"\")] [EventStreamType(\"All\")]")]
    [InlineData("[EventStreamId(\"Elsewhere\")]")]
    public void should_not_infer_observation_from_an_append_identifier_or_unrestricted_filter(string attributes)
    {
        GenerateFilteredWith($$"""{{attributes}} public class Welcomer : IReactor { {{ReturnedCommand}} }""");

        Result.Source.ShouldNotContain("from ");
        Result.Source.ShouldContain("invokes SendWelcome");
        AssertDocument();
    }
}
