// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

public class and_a_document_does_not_bind : Specification
{
    const string Source = "domain Library\n" +
        "module Library\n" +
        "  feature Authors\n" +
        "    slice StateView Listing\n" +
        "      query AuthorById => Author optional\n" +
        "        by id String";

    EmbeddedDocumentGeneration _generation;

    void Because()
    {
        var emitter = Substitute.For<IScreenplayEmitter>();
        emitter.Emit(Arg.Any<Model.ApplicationModel>(), Arg.Any<ScreenplayOptions>())
            .Returns(new ScreenplayEmission(Source, null!, []));
        _generation = new EmbeddedDocumentGenerator(new ApplicationModelAnalyzer(), emitter, new ScreenplayVerifier())
            .Generate(given.an_application.Build(), given.an_application.Options(), []);
    }

    [Fact] void should_report_each_document_binding_error() => _generation.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldEqual(5);
    [Fact] void should_report_only_information_to_preserve_consumer_builds() => _generation.Diagnostics.All(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Information).ShouldBeTrue();
    [Fact] void should_identify_the_document_scope() => _generation.Diagnostics.Select(diagnostic => diagnostic.Location).ShouldContain(given.an_application.NestedFeature);
    [Fact] void should_include_the_binder_message() => _generation.Diagnostics.All(diagnostic => diagnostic.Message.Contains("read model or key property is unresolved", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_still_return_every_document() => _generation.Documents.Count.ShouldEqual(5);
}
