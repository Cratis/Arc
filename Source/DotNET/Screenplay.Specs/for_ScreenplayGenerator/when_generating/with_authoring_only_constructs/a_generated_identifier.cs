// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class a_generated_identifier : an_authoring_document
{
    void Because() => Generate(IdentifierSources.With("""
        [Command]
        public record RegisterAuthor(string Name)
        {
            public (AuthorId, AuthorRegistered) Handle()
            {
                AuthorId authorId = new(Guid.NewGuid());
                return (authorId, new(Name));
            }
        }
        """));

    [Fact]
    void should_type_concrete_specification_sources_using_the_generated_identifier()
    {
        const string First = "11111111-1111-1111-1111-111111111111";
        const string Second = "22222222-2222-2222-2222-222222222222";
        var model = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Specifications = [new(
                    "TwoAuthors",
                    [new("AuthorRegistered", SpecificationStateKind.Event, [new("Name", new LiteralSource("First"))]) { For = new(First) }],
                    null,
                    [new("AuthorRegistered", SpecificationStateKind.Event, [new("Name", new LiteralSource("Second"))]) { For = new(Second) }],
                    [])]
            }).ToList()
        };
        var emitted = new ScreenplayEmitter().Emit(model, new() { AuthoringOnlyConstructs = true });
        emitted.Source.ShouldContain("specification TwoAuthors");
        emitted.Source.ShouldContain($"for \"{First}\"");
        emitted.Source.ShouldContain($"for \"{Second}\"");
        emitted.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
        var defaultModel = model with
        {
            Slices = model.Slices.Select(slice => slice with { Commands = slice.Commands.Select(command => command with { Authoring = null }).ToList() }).ToList()
        };
        var defaultEmission = new ScreenplayEmitter().Emit(defaultModel, new());
        defaultEmission.Source.Contains("specification TwoAuthors", StringComparison.Ordinal).ShouldBeFalse();
        defaultEmission.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    }

    [Fact] void should_emit_the_generated_identifier() => Result.Source.ShouldContain("authorId AuthorId generated identifier");
    [Fact] void should_return_the_generated_identifier() => Result.Source.ShouldContain("returns authorId");
    [Fact] void should_route_the_event_to_the_returned_identity() => Result.Source.ShouldContain("for authorId");
    [Fact] void should_retire_the_old_diagnostic_for_this_shape() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).ShouldBeEmpty();
    [Fact] void should_compile_and_reject_only_executable_admission() => AssertAuthoringDocument();
    [Fact] void should_not_generate_or_return_an_identity_by_default() => Off.Source.Contains("generated", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_opt_in_when_disabled() => Off.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).Message.ShouldContain("AuthoringOnlyConstructs");
}
