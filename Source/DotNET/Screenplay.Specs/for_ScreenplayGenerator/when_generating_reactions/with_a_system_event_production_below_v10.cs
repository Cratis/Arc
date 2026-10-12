// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_a_system_event_production_below_v10 : a_reacting_application
{
    const string Reactor = """
        [ExecuteCommandsAsSystem("Librarian")]
        public class Welcomer : IReactor
        {
            public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, "Welcome");
        }
        """;

    void Because() => Generate(
        new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V9 },
        ("Library/Authors/Registration/Registration.cs", Registration),
        ("Library/Authors/Welcoming/Welcoming.cs", Welcoming + Reactor),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_keep_the_declarative_event_production() => Result.Source.ShouldContain("produces AuthorWelcomed");
    [Fact] void should_not_fall_back_to_a_file() => Result.Source.ShouldNotContain("file Authors/Welcoming/Welcoming.cs");
    [Fact] void should_withhold_the_system_identity() => Result.Source.ShouldNotContain("runs as");
    [Fact] void should_report_the_withheld_metadata() => Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.ReactionMetadataVersionCap);
    [Fact] void should_compile_and_round_trip() => AssertDocument();
}
