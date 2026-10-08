// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

public class with_binding_only_for_the_final_assembly_document : Specification
{
    recording_verifier _verifier;
    EmbeddedDocumentGeneration _generation;

    void Establish() => _verifier = new();

    void Because() => _generation = new EmbeddedDocumentGenerator(new ApplicationModelAnalyzer(), new ScreenplayEmitter(), _verifier)
        .Generate(given.an_application.Build(), given.an_application.Options(), []);

    [Fact] void should_bind_the_whole_application_once() => _verifier.BoundSources.Count.ShouldEqual(1);
    [Fact] void should_bind_the_final_arranged_document() => _verifier.BoundSources.Single().ShouldEqual(_generation.Documents.Single(document => document.Document.Id == given.an_application.RootNamespace).Source);
    [Fact] void should_compile_each_scoped_document_without_binding() => _verifier.SyntaxSources.Count.ShouldEqual(4);
    [Fact] void should_not_report_defects_for_valid_cross_scope_imports() => _generation.Diagnostics.ShouldBeEmpty();

    /// <summary>
    /// Records verification calls because Castle/NSubstitute cannot intercept the default interface method.
    /// </summary>
    sealed class recording_verifier : IScreenplayVerifier
    {
        readonly ScreenplayVerifier _verifier = new();
        readonly List<string> _boundSources = [];
        readonly List<string> _syntaxSources = [];

        public IReadOnlyList<string> BoundSources => _boundSources;
        public IReadOnlyList<string> SyntaxSources => _syntaxSources;

        public ScreenplayVerification Verify(string source)
        {
            _boundSources.Add(source);

            return _verifier.Verify(source);
        }

        public ScreenplayVerification VerifySyntax(string source)
        {
            _syntaxSources.Add(source);

            return _verifier.VerifySyntax(source);
        }
    }
}
