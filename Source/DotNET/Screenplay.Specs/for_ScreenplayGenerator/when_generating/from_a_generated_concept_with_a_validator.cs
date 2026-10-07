// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_generated_concept_with_a_validator : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        public class AuthorIdValidator : Cratis.Arc.Validation.ConceptValidator<AuthorId>
        {
        }
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorId, AuthorRegistered) Handle()
            {
                AuthorId authorId = new(Guid.NewGuid());
                return (authorId, new(Name));
            }
        }
        """)));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_omit_the_command_unless_the_blocked_payload_member_is_optional(bool optional)
    {
        var source = IdentifierSources.With("""
            public class AuthorIdValidator : Cratis.Arc.Validation.ConceptValidator<AuthorId> { }
            [Command] public record RegisterAuthor(string Name)
            {
                public (AuthorId, AuthorRegistered) Handle()
                {
                    AuthorId authorId = new(Guid.NewGuid());
                    return (authorId, new(authorId, Name));
                }
            }
            """).Replace("public record AuthorRegistered(string Name);", $"public record AuthorRegistered(AuthorId{(optional ? "?" : string.Empty)} Copy, string Name);", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain("copy AuthorId");
        Result.Source.ShouldNotContain("copy = authorId");
        Result.Source.ShouldNotContain("produces event AuthorRegistered");
        if (optional)
        {
            Result.Source.ShouldContain("produces AuthorRegistered");
            Result.Source.ShouldContain("name = name");
            Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("production was retained", StringComparison.Ordinal)).ShouldBeTrue();
        }
        else
        {
            Result.Source.ShouldNotContain("command RegisterAuthor");
            Result.Source.ShouldNotContain("produces AuthorRegistered");
            var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse && diagnostic.Message.Contains("command was left out", StringComparison.Ordinal));
            diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
            diagnostic.Message.ShouldContain("production 'AuthorRegistered' needs the generated value 'authorId'");
            var withScenario = Result.Model with
            {
                Slices = Result.Model.Slices.Select(slice => slice with
                {
                    Specifications = [new("Registering", [], new("RegisterAuthor", SpecificationStateKind.Command, [new("Name", new LiteralSource("Austen"))]), [], [])]
                }).ToList()
            };
            var emitted = new ScreenplayEmitter().Emit(withScenario, new());
            emitted.Source.ShouldNotContain("specification Registering");
            emitted.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("command is not present");
        }
        AssertDocument();
    }

    [Fact] void should_not_generate_a_validated_concept() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_not_return_an_unavailable_value() => Result.Source.ShouldNotContain("returns authorId");
    [Fact] void should_not_inline_or_retarget_the_event() => Result.Source.ShouldNotContain("for authorId");
    [Fact] void should_report_the_response_omission() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.ShouldContain("concept validators");
    [Fact] void should_report_the_identity_omission() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).ShouldBeTrue();
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_not_select_v7() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
}
