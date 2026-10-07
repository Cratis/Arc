// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unadmitted_generated_shapes : a_generated_document
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

    [Fact] void should_not_generate_optional_values() => AssertOmitted(new("AuthorId", false, true));
    [Fact] void should_not_generate_collections() => AssertOmitted(new("AuthorId", true, false));
    [Fact] void should_not_generate_bare_uuid_values() => AssertOmitted(new("Uuid", false, false));

    [Fact] void should_not_generate_a_concept_with_declarative_or_named_rules() => AssertOmitted(new("AuthorId", false, false), true);

    void AssertOmitted(TypeReferenceModel type, bool withRule = false)
    {
        var model = Result.Model with
        {
            Concepts = Result.Model.Concepts.Select(concept => withRule && concept.Name == "AuthorId" ? concept with
            {
                Validations = [new("Value", ValidationRuleKind.Rule, "KnownIdentity", null) { SourceFilePath = "Rules/KnownIdentity.cs" }]
            } : concept).ToList(),
            Slices = Result.Model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => command with
                {
                    Authoring = command.Authoring! with { Generated = [new("authorId", type)] }
                }).ToList()
            }).ToList()
        };
        var emitted = new ScreenplayEmitter().Emit(model, new());
        emitted.Source.ShouldNotContain("generated");
        emitted.Source.ShouldNotContain("returns authorId");
        emitted.Source.ShouldNotContain("for authorId");
        emitted.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.ShouldContain("PLAY0268");
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var source = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", emitted.Source);
        var bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([source], catalog));
        Assert.True(bound.Success, string.Join(Environment.NewLine, bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
        if (type.Name == "AuthorId")
        {
            emitted.Source.ShouldNotContain("concept AuthorId");
        }
    }
}
