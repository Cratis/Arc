// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_event_remarks_with_method_references : a_generated_document
{
    EventSyntax _event;

    void Because()
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            [Command]
            public record RegisterAuthor([Key] Guid Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            public static class Names
            {
                public static string Normalize(string name) => name;
                public static T Keep<T>(T value) => value;
            }
            """).Replace("[EventType]", "/// <remarks>Use <see cref=\"Names.Normalize(string)\"/> and <see cref=\"Names.Keep{T}(T)\"/>.</remarks>\n[EventType]", StringComparison.Ordinal)));
        _event = new ScreenplayCompiler().Compile(Result.Source).Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().InlineEvent!;
    }

    [Fact] void should_keep_only_the_method_names() => _event.Documentation.ShouldEqual("Use `Normalize` and `Keep`.");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
