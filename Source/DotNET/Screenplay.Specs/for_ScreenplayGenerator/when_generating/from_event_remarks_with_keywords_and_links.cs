// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_event_remarks_with_keywords_and_links : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command] public record RegisterAuthor([Key] Guid Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """).Replace("[EventType]", "/// <remarks>Use <see langword=\"null\"/>, <see langword=\"true\"/> and <see langword=\"false\"/>. See <see href=\"https://example.com\"/> or <see href=\"https://example.com/details\">the details</see>.</remarks>\n[EventType]", StringComparison.Ordinal)));

    [Fact]
    void should_retain_keywords_and_links() => new ScreenplayCompiler().Compile(Result.Source).Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().InlineEvent!
        .Documentation.ShouldEqual("Use `null`, `true` and `false`. See https://example.com or the details.");

    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
