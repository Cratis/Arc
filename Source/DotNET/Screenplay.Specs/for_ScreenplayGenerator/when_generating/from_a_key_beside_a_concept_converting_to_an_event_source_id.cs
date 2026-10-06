// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A convertible concept does not make the actual key ambiguous.
/// </summary>
public class from_a_key_beside_a_concept_converting_to_an_event_source_id : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        public record CorrelationId(Guid Value) : Cratis.Concepts.ConceptAs<Guid>(Value)
        {
            public static implicit operator EventSourceId(CorrelationId value) => new(value.Value.ToString());
        }

        [Command]
        public record RegisterAuthor([Key] Guid Id, CorrelationId Correlation, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_use_the_key() => Result.Source.ShouldContain("id Uuid identifier");
    [Fact] void should_state_the_keys_destination() => Result.Source.ShouldContain("for id");
    [Fact] void should_not_report_ambiguity() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.AmbiguousCommandIdentifier).ShouldBeFalse();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
