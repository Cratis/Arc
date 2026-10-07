// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_reordered_named_inline_event_arguments : a_generated_document
{
    CompilationResult<SemanticCompilation> _standalone;
    string _standaloneSource;

    void Because()
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            [Command]
            public record RegisterAuthor(AuthorId Id)
            {
                public AuthorRegistered Handle() => new(FamilyName: "Austen", GivenName: "Jane");
            }
            """).Replace("AuthorRegistered(string Name)", "AuthorRegistered(string GivenName, string FamilyName)", StringComparison.Ordinal)));
        _standaloneSource = new ScreenplayEmitter().Emit(Result.Model with { EventProducerCounts = new Dictionary<string, int>() }, new ScreenplayOptions()).Source;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        const string Key = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "application.play", _standaloneSource);
        _standalone = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
    }

    [Fact] void should_declare_the_event_inline() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_map_the_given_name() => Result.Source.ShouldContain("givenName String = \"Jane\"");
    [Fact] void should_map_the_family_name() => Result.Source.ShouldContain("familyName String = \"Austen\"");
    [Fact] void should_keep_named_standalone_mappings() => _standaloneSource.ShouldContain("familyName = \"Austen\"");
    [Fact] void should_keep_the_standalone_given_name_mapping() => _standaloneSource.ShouldContain("givenName = \"Jane\"");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
    [Fact] void should_bind_the_standalone_form() => _standalone.Success.ShouldBeTrue();
    [Fact] void should_preserve_canonical_esm() => SemanticModelSerializer.Serialize(Bound.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_standalone.Value!.Model)).ShouldBeTrue();

    [Fact]
    public void should_keep_colliding_emitted_property_names_standalone()
    {
        var slice = Result.Model.Slices.Single();
        var command = slice.Commands.Single();
        var production = command.Produces.Single();
        var declaration = slice.Events.Single();
        var names = new Dictionary<string, string> { ["GivenName"] = "URL", ["FamilyName"] = "Url" };
        var model = Result.Model with
        {
            Slices = [slice with
            {
                Events = [declaration with { Properties = declaration.Properties.Select(property => property with { Name = names[property.Name] }).ToList() }],
                Commands = [command with { Produces = [production with { Mappings = production.Mappings.Select(mapping => mapping with { Property = names[mapping.Property] }).ToList() }] }]
            }]
        };

        var source = new ScreenplayEmitter().Emit(model, new ScreenplayOptions()).Source;
        source.ShouldNotContain("produces event AuthorRegistered");
        source.ShouldContain("event AuthorRegistered");
        source.ShouldContain("produces AuthorRegistered");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_incomplete_or_unknown_mappings_standalone(bool unknown)
    {
        var slice = Result.Model.Slices.Single();
        var command = slice.Commands.Single();
        var production = command.Produces.Single();
        var mappings = production.Mappings.ToList();
        if (unknown)
        {
            mappings[0] = mappings[0] with { Property = "Unknown" };
        }
        else
        {
            mappings.RemoveAt(0);
        }

        var model = Result.Model with { Slices = [slice with { Commands = [command with { Produces = [production with { Mappings = mappings }] }] }] };
        var source = new ScreenplayEmitter().Emit(model, new ScreenplayOptions()).Source;
        source.ShouldNotContain("produces event AuthorRegistered");
        source.ShouldContain("event AuthorRegistered");
        source.ShouldContain("produces AuthorRegistered");
    }
}
