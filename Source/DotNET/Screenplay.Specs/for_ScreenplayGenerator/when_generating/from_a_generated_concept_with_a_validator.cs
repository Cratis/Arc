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

    [Fact]
    public void should_keep_the_legacy_production_without_the_blocked_optional_mapping()
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
            """).Replace("public record AuthorRegistered(string Name);", "public record AuthorRegistered(AuthorId? Copy, string Name);", StringComparison.Ordinal);
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldContain("copy AuthorId");
        Result.Source.ShouldNotContain("copy = authorId");
        Result.Source.ShouldNotContain("produces event AuthorRegistered");
        Result.Source.ShouldContain("command RegisterAuthor");
        Result.Source.ShouldContain("produces AuthorRegistered");
        Result.Source.ShouldContain("name = name");
        Result.Source.ShouldNotContain("generated");
        Result.Source.ShouldNotContain("returns");
        Result.Source.ShouldNotContain("handler");
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction);
        diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
        diagnostic.Message.ShouldContain("AuthorRegistered.Copy");
        var legacyModel = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => command with
                {
                    Authoring = null,
                    Produces = command.Produces.Select(production => production with
                    {
                        Mappings = production.Mappings.Where(mapping => mapping.Property != "Copy").ToList(),
                        CanInline = false,
                        UsesCommandContext = false
                    }).ToList()
                }).ToList()
            }).ToList()
        };
        new ScreenplayEmitter().Emit(legacyModel, new()).Source.ShouldEqual(Result.Source);
        RoundTrip.IsStable.ShouldBeTrue();
        RoundTrip.Errors.ShouldBeEmpty();
        AssertDocument();

        var withScenario = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Specifications = [new("Registering", [], new("RegisterAuthor", SpecificationStateKind.Command, [new("Name", new LiteralSource("Austen"))]), [], [])]
            }).ToList()
        };
        var emitted = new ScreenplayEmitter().Emit(withScenario, new());
        emitted.Source.ShouldContain("specification Registering");
        emitted.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
    }

    [Fact] void should_not_generate_a_validated_concept() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_not_return_an_unavailable_value() => Result.Source.ShouldNotContain("returns authorId");
    [Fact] void should_not_inline_or_retarget_the_event() => Result.Source.ShouldNotContain("for authorId");
    [Fact] void should_report_the_response_omission() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.ShouldContain("concept validators");
    [Fact] void should_report_the_identity_omission() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).ShouldBeTrue();
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_not_select_v7() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
}
