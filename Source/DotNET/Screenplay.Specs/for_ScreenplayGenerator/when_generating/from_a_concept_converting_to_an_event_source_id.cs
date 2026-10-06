// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// An implicit conversion is not Chronicle's runtime event-source key contract.
/// </summary>
public class from_a_concept_converting_to_an_event_source_id : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        public record CorrelationId(Guid Value) : Cratis.Concepts.ConceptAs<Guid>(Value)
        {
            public static implicit operator EventSourceId(CorrelationId value) => new(value.Value.ToString());
        }

        [Command]
        public record RegisterAuthor(CorrelationId Correlation, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_not_invent_an_identifier() => Result.Source.ShouldNotContain("identifier");
    [Fact] void should_not_invent_a_destination() => Result.Source.ShouldNotContain("for correlation");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
