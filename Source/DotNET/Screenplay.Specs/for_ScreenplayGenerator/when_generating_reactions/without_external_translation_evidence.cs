// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class without_external_translation_evidence : a_reacting_application
{
    const string ReturnedEvent = "public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, \"Welcome\");";
    const string ReturnedCommand = "public Task<SendWelcome> Welcome(AuthorRegistered @event) => Task.FromResult(new SendWelcome(@event.Name, @event.Country));";

    [Theory]
    [InlineData(ReturnedEvent, "", "", "produces AuthorWelcomed")]
    [InlineData(ReturnedCommand, "", "", "invokes SendWelcome")]
    [InlineData(ReturnedCommand, "[ExecuteCommandsAsSystem(\"Librarian\")]", "", "runs as system")]
    [InlineData("public PublicWelcome Welcome(AuthorRegistered @event) => new(@event.Name);", "", "[EventType, Public] public record PublicWelcome(string Name);", "reaction Welcomer")]
    [InlineData(ReturnedEvent, "[Reactor(eventSequence: \"inbox\")]", "", "reaction Welcomer")]
    public void should_remain_an_automation(string handler, string attributes, string declarations, string expectedEffect)
    {
        GenerateWith($$"""
            {{declarations}}
            {{attributes}}
            public class Welcomer : IReactor { {{handler}} }
            """);

        var slice = Result.Model.Slices.Single(slice => slice.Name == "Welcoming");
        slice.Kind.ShouldEqual(SliceKind.Automation);
        slice.Reactors.Single().IsTranslating.ShouldBeTrue();
        Result.Source.ShouldContain(expectedEffect);
        var parsed = new ScreenplayCompiler().Compile(Result.Source).Value!;
        parsed.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .Single(slice => slice.Name == "Welcoming").Type.ShouldEqual(SliceType.Automation);
        AssertDocument();
    }
}
