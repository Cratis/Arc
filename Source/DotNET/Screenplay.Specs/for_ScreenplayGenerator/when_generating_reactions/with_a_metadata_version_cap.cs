// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_a_metadata_version_cap : a_reacting_application
{
    [Theory]
    [InlineData(false, "[ExecuteCommandsAsSystem]")]
    [InlineData(true, "[ExecuteCommandsAsSystem]")]
    [InlineData(false, "[Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>(\"Onboarding\")]")]
    [InlineData(true, "[Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>(\"Onboarding\")]")]
    public void should_withhold_metadata_dependent_effects_in_both_modes(bool authoring, string attributes)
    {
        var reactor = $$"""
            [Cratis.Chronicle.EventSources.EventSource]
            [Cratis.Chronicle.EventSources.EventStream("Onboarding")]
            public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;
            {{attributes}}
            public class Welcomer : IReactor
            {
                public Task<SendWelcome> Welcome(AuthorRegistered @event) => Task.FromResult(new SendWelcome(@event.Name, @event.Country));
            }
            """;
        Generate(
            new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = SemanticVersion.V9 },
            ("Library/Authors/Registration/Registration.cs", Registration),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming + reactor),
            (IntegrationTesting.Path, IntegrationTesting.Source));

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("invokes SendWelcome");
        Result.Source.ShouldNotContain("runs as");
        Result.Source.ShouldNotContain("from Author");
        Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.ReactionMetadataVersionCap);
        AssertDocument();
    }
}
