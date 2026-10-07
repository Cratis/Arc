// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class a_response_record : an_authoring_document
{
    void Because() => Generate(IdentifierSources.With("""
        public record LineId(Guid Value) : Cratis.Concepts.ConceptAs<Guid>(Value);
        public record RegisterAuthorResponse(LineId LineId, string Name);
        [Command]
        public record RegisterAuthor(string Name)
        {
            public (RegisterAuthorResponse, AuthorRegistered) Handle()
            {
                var lineId = new LineId(Guid.NewGuid());
                return (new RegisterAuthorResponse(lineId, Name), new(Name));
            }
        }
        """));

    [Fact] void should_generate_a_non_identifier_uuid_concept() => Result.Source.ShouldContain("lineId LineId generated");
    [Fact] void should_not_mark_the_line_identity_as_the_event_source() => Result.Source.Contains("generated identifier", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_recover_the_inferred_local_as_required() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Generated.Single().Type.IsOptional.ShouldBeFalse();
    [Fact] void should_return_the_readable_fields() => Result.Source.ShouldContain("lineId = lineId");
    [Fact] void should_return_command_input() => Result.Source.ShouldContain("name = name");
    [Fact] void should_emit_a_record_response() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.ResponseFields.Count.ShouldEqual(2);
    [Fact] void should_bind_both_modes_as_v7() => AssertExecutableDocument();
    [Fact] void should_emit_responses_by_default() => Off.Source.ShouldContain("returns");
    [Fact] void should_generate_values_by_default() => Off.Source.ShouldContain("lineId LineId generated");
}
