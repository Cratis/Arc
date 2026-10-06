// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// An optional key is not admitted as an explicit command destination.
/// </summary>
public class from_a_command_with_an_optional_key : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public record RegisterAuthor([Key] Guid? Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_preserve_the_optional_input() => Result.Source.ShouldContain("id Uuid optional");
    [Fact] void should_not_emit_an_unbindable_identifier() => Result.Source.ShouldNotContain(" identifier");
    [Fact] void should_report_the_unsupported_identity_shape() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandIdentifier).ShouldBeTrue();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
