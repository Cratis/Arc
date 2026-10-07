// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_generated_values_with_pre_generation_rules : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorId, AuthorRegistered) Handle()
            {
                AuthorId authorId = new(Guid.NewGuid());
                return (authorId, new(Name));
            }
        }
        """)));

    [Fact] void should_fall_back_for_a_property_rule_on_generation() => AssertFallback(command => command with
    {
        Validations = [new("authorId", ValidationRuleKind.NotEmpty, null, "Required")]
    });

    [Fact] void should_fall_back_for_a_rule_operand_reading_generation() => AssertFallback(command => command with
    {
        Validations = [new("Name", ValidationRuleKind.Equal, new PropertyPathSource("authorId"), "Required")]
    });

    [Fact] void should_fall_back_for_a_requirement_reading_generation() => AssertFallback(command => command with
    {
        Authoring = command.Authoring! with { Requirements = [new(new ComparisonCondition("authorId", ComparisonKind.Equal, new LiteralSource("id")), "Required")] }
    });

    void AssertFallback(Func<CommandModel, CommandModel> modify)
    {
        var model = Result.Model with
        {
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(modify).ToList(),
                Specifications = [new("RegisteringAnAuthor", [], new("RegisterAuthor", SpecificationStateKind.Command, [new("Name", new LiteralSource("Apollo"))]), [new("AuthorRegistered", SpecificationStateKind.Event, [new("Name", new LiteralSource("Apollo"))])], [])]
            }).ToList()
        };
        var emitted = new ScreenplayEmitter().Emit(model, new());
        emitted.Source.ShouldContain("command RegisterAuthor");
        emitted.Source.ShouldContain("produces AuthorRegistered");
        emitted.Source.ShouldContain("name = name");
        emitted.Source.ShouldContain("specification RegisteringAnAuthor");
        emitted.Source.ShouldNotContain("generated");
        emitted.Source.ShouldNotContain("returns");
        emitted.Source.ShouldNotContain("handler");
        emitted.Source.ShouldNotContain("authorId not empty");
        emitted.Source.ShouldNotContain("name == authorId");
        emitted.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
        emitted.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).ShouldBeTrue();
        emitted.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.ShouldContain("PLAY0273");
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var source = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", emitted.Source);
        var bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([source], catalog));
        Assert.True(bound.Success, string.Join(Environment.NewLine, bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    }
}
