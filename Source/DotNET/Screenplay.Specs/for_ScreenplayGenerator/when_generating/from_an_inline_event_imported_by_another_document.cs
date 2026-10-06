// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_inline_event_imported_by_another_document : a_generated_document
{
    const string Consumer = """
        import "producer.play"
        module Library
          feature Authors
            slice StateView Listing
              readmodel Authors
                name String
              projection Authors => Authors
                from AuthorRegistered key name
                  name = name
              query AuthorByName => Authors optional
                by name String
        """;

    CompilationResult<SemanticCompilation> _documents;

    void Because()
    {
        Generate((Analyzed.SlicePath, IdentifierSources.With("""
            [Command]
            public record RegisterAuthor(AuthorId Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """)));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var producer = SemanticSourceDocument.Create(catalog.ResolveDocument("producer"), "producer", "producer.play", Result.Source);
        var consumer = SemanticSourceDocument.Create(catalog.ResolveDocument("consumer"), "consumer", "consumer.play", Consumer);
        _documents = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([consumer, producer], catalog));
    }

    [Fact] void should_declare_the_event_inline() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_resolve_the_imported_inline_event() => _documents.Success.ShouldBeTrue();
    [Fact] void should_bind_without_errors() => _documents.Diagnostics.Where(_ => _.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).Select(_ => _.Message).ShouldBeEmpty();
    [Fact] void should_bind_without_warnings() => _documents.Diagnostics.Where(_ => _.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).Select(_ => _.Message).ShouldBeEmpty();
}
