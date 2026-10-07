// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_single_event_producer : a_generated_document
{
    CompilationResult<SemanticCompilation> _standalone;

    void Because()
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            [Command]
            public record RegisterAuthor(AuthorId Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """)));
        var source = new ScreenplayEmitter().Emit(Result.Model with { EventProducerCounts = new Dictionary<string, int>() }, new ScreenplayOptions()).Source;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        const string Key = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "application.play", source);
        _standalone = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
    }

    [Fact] void should_declare_the_event_inline() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_emit_typed_mappings() => Result.Source.ShouldContain("name String = name");
    [Fact] void should_keep_explicit_routing() => Result.Source.ShouldContain("for id");
    [Fact] void should_keep_the_event_in_the_analysis_model() => Result.Model.Slices.SelectMany(_ => _.Events).Single().Name.ShouldEqual("AuthorRegistered");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
    [Fact] void should_bind_without_warnings() => Bound.Diagnostics.Where(_ => _.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
    [Fact] void should_bind_the_standalone_form() => _standalone.Success.ShouldBeTrue();
    [Fact] void should_preserve_canonical_esm() => SemanticModelSerializer.Serialize(Bound.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_standalone.Value!.Model)).ShouldBeTrue();
}
