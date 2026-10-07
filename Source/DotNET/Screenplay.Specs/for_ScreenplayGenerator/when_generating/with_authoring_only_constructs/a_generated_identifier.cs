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
        var diagnostic = defaultEmission.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);
        diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
        diagnostic.Location.ShouldEqual(Result.Model.Slices.Single().Namespace);
    }

    [Fact]
    void should_withhold_generation_to_keep_success_scenarios_without_fixtures()
    {
        var model = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Specifications = [new("RegisteringAnAuthor", [], new("RegisterAuthor", SpecificationStateKind.Command, [new("Name", new LiteralSource("Apollo"))]), [new("AuthorRegistered", SpecificationStateKind.Event, [new("Name", new LiteralSource("Apollo"))])], [])]
            }).ToList()
        };
        var emitted = new ScreenplayEmitter().Emit(model, new());
        emitted.Source.ShouldContain("specification RegisteringAnAuthor");
        emitted.Source.ShouldNotContain("generated");
        emitted.Source.ShouldNotContain("returns");
        emitted.Source.ShouldContain("produces AuthorRegistered");
        emitted.Source.ShouldNotContain("concept AuthorId");
        var legacy = model with
        {
            Concepts = model.Concepts.Where(concept => concept.Name != "AuthorId").ToList(),
            Slices = model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => command with { Authoring = null }).ToList()
            }).ToList()
        };
        emitted.Source.ShouldEqual(new ScreenplayEmitter().Emit(legacy, new()).Source);
        emitted.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
        emitted.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.ShouldContain("withheld to keep successful scenarios");
    }

    [Fact] void should_emit_the_generated_identifier() => Result.Source.ShouldContain("authorId AuthorId generated identifier");
    [Fact] void should_return_the_generated_identifier() => Result.Source.ShouldContain("returns authorId");
    [Fact] void should_route_the_event_to_the_returned_identity() => Result.Source.ShouldContain("for authorId");
    [Fact] void should_retire_the_old_diagnostic_for_this_shape() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).ShouldBeEmpty();
    [Fact] void should_bind_both_modes_as_v7() => AssertExecutableDocument();
    [Fact] void should_generate_and_return_an_identity_by_default() => Off.Source.ShouldContain("authorId AuthorId generated identifier");
    [Fact] void should_not_suggest_opt_in_for_admitted_values() => Off.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult || diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeEmpty();
}
