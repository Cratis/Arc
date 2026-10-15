// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_identifier_in_an_event_for_event_source_id : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, GeneratedValueSources.With("""
        [Command] public record RegisterAuthor(string Name)
        {
            public Cratis.Chronicle.EventSequences.EventForEventSourceId Handle([GeneratedValue("author")] AuthorId authorId) => new(authorId, new AuthorRegistered(Name));
        }
        """)));

    [Fact] void should_recover_the_explicit_destination() => Result.Source.ShouldContain("author AuthorId generated identifier");
    [Fact] void should_route_the_inline_event_to_the_parameter() => Result.Source.ShouldContain("for author");
    [Fact] void should_inline_the_event() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_not_invent_a_response_from_the_wrapper() => Result.Source.ShouldNotContain("returns");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_keep_the_original_unrouted_version() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_keep_default_routed_generated_wrappers_in_command_context(bool authoring, bool definition)
    {
        AssertUnchangedRouting(authoring, definition, string.Empty);
        AssertUnchangedRouting(authoring, definition, string.Empty, SemanticVersion.V8);
    }

    [Theory]
    [InlineData(false, " ")]
    [InlineData(true, " ")]
    [InlineData(false, "EventSourceType = \"Account\", EventStreamType = \"Main\"")]
    [InlineData(true, "EventSourceType = \"Account\", EventStreamType = \"Main\"")]
    [InlineData(true, "EventSource = typeof(AccountEventSource), EventStream = \"Main\"")]
    [InlineData(false, "EventSourceType = \"Default\", EventStreamType = \"All\", EventStreamId = \"Default\"")]
    public void should_not_emit_an_override_for_unchanged_effective_metadata(bool definition, string metadata)
    {
        foreach (var authoring in new[] { false, true })
        {
            AssertUnchangedRouting(authoring, definition, metadata);
        }
    }

    void AssertUnchangedRouting(bool authoring, bool definition, string metadata, SemanticVersion? cap = null)
    {
        var initializer = metadata.Length == 0 ? string.Empty : $" {{ {metadata} }}";
        var route = definition ? "Cratis.Arc.Chronicle.Commands.EventSource<AccountEventSource>(\"Main\")" : "EventSourceType(\"Account\"), EventStreamType(\"Main\")";
        var source = GeneratedValueSources.With($$"""
            [Cratis.Chronicle.EventSources.EventSource("Account")]
            [Cratis.Chronicle.EventSources.EventStream("Main")]
            public class AccountEventSource : Cratis.Chronicle.EventSources.IEventSource;
            [Command, {{route}}] public record RegisterAuthor(string Name)
            {
                public Cratis.Chronicle.EventSequences.EventForEventSourceId Handle([GeneratedValue("author")] AuthorId authorId) => new(authorId, new AuthorRegistered(Name)){{initializer}};
            }
            """);
        var options = new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = cap };
        Generate(options, (Analyzed.SlicePath, source));
        var baseline = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => command with
                {
                    Produces = command.Produces.Select(production => production with { UsesCommandContext = true, Destination = null, Route = null }).ToList()
                }).ToList()
            }).ToList()
        };
        Result.Source.ShouldEqual(new ScreenplayEmitter().Emit(baseline, options).Source);
        var production = Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single();
        production.UsesCommandContext.ShouldBeTrue();
        production.Route.ShouldBeNull();
        production.Destination.ShouldBeNull();
        var bound = Bound.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        bound.Route.ShouldNotBeNull();
        bound.Produces.Single().Route.ShouldBeNull();
        Bound.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
        AssertDocument();
    }
}
