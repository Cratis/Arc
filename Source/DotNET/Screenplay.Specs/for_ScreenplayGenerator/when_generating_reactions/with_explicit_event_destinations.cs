// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_explicit_event_destinations : a_filtered_application
{
    const string Definition = "[Cratis.Chronicle.EventSources.EventSource(\"Author\")] [Cratis.Chronicle.EventSources.EventStream(\"Onboarding\")] public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;";
    const string ReturnedEvent = "public Task<Cratis.Chronicle.EventSequences.EventForEventSourceId> Welcome(AuthorRegistered @event) => Task.FromResult(new Cratis.Chronicle.EventSequences.EventForEventSourceId(\"author\", new AuthorWelcomed(@event.Name, \"Welcome\")));";

    [Theory]
    [InlineData("[EventSourceType(\"Author\")]", "from Author\n")]
    [InlineData("[Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>(\"Onboarding\")]", "from Author.Onboarding")]
    public void should_preserve_the_observer_filter_without_classifying_explicit_destinations_as_context_routing(string attributes, string filter)
    {
        GenerateFilteredWith($$"""{{Definition}} {{attributes}} public class Welcomer : IReactor { {{ReturnedEvent}} }""");

        Result.Source.ShouldContain(filter);
        Result.Source.ShouldContain("produces AuthorWelcomed");
        Result.Source.ShouldContain("for \"author\"");
        Result.Source.ShouldNotContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableReactionAppendRoute).ShouldBeEmpty();
        AssertDocument();
    }

    [Theory]
    [InlineData(false, "[EventSourceType(\"Author\")]")]
    [InlineData(true, "[EventSourceType(\"Author\")]")]
    [InlineData(false, "[Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>(\"Onboarding\")]")]
    [InlineData(true, "[Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>(\"Onboarding\")]")]
    public void should_withhold_the_filter_below_v10_in_both_modes(bool authoring, string attributes)
    {
        Generate(
            new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = SemanticVersion.V9 },
            ("Library/Authors/Registration/Registration.cs", "using Cratis.Chronicle.Events; namespace Library.Authors.Registration; [EventType] public record AuthorRegistered(string Name, string Country);"),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming + $$"""{{Definition}} {{attributes}} public class Welcomer : IReactor { {{ReturnedEvent}} }"""),
            (IntegrationTesting.Path, IntegrationTesting.Source));

        Result.Source.ShouldNotContain("from Author");
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.ReactionMetadataVersionCap);
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableReactionAppendRoute).ShouldBeEmpty();
        AssertDocument();
    }
}
