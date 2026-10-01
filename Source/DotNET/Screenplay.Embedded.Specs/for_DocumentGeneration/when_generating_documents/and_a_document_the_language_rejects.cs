// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

/// <summary>
/// A document the Screenplay compiler rejects is output nobody can use, and embedding it would turn a build problem
/// into one that only shows up inside a running application. Every generated document is read back, and anything
/// rejected is reported as an error - which is what makes the build that produced it fail.
/// </summary>
public class and_a_document_the_language_rejects : Specification
{
    const string Rejected = "this is not a Screenplay document";

    IScreenplayEmitter _emitter;
    EmbeddedDocumentGeneration _generation;

    void Establish()
    {
        _emitter = Substitute.For<IScreenplayEmitter>();
        _emitter
            .Emit(Arg.Any<Model.ApplicationModel>(), Arg.Any<ScreenplayOptions>())
            .Returns(new ScreenplayEmission(Rejected, null!, []));
    }

    void Because() => _generation = new EmbeddedDocumentGenerator(new ApplicationModelAnalyzer(), _emitter, new ScreenplayVerifier())
        .Generate(given.an_application.Build(), given.an_application.Options(), []);

    [Fact] void should_not_be_successful() => _generation.IsSuccess.ShouldBeFalse();

    [Fact] void should_report_that_a_document_did_not_compile() =>
        _generation.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.DocumentDidNotCompile);

    [Fact] void should_report_every_document_it_rejected() =>
        _generation.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.DocumentDidNotCompile).ShouldEqual(5);

    [Fact] void should_say_which_document_was_rejected() =>
        _generation.Diagnostics
            .Where(_ => _.Code == ScreenplayDiagnosticCodes.DocumentDidNotCompile)
            .Select(_ => _.Location)
            .ShouldContain(given.an_application.NestedFeature);

    [Fact] void should_report_it_as_an_error() =>
        _generation.Diagnostics
            .Where(_ => _.Code == ScreenplayDiagnosticCodes.DocumentDidNotCompile)
            .All(_ => _.Severity == ScreenplayDiagnosticSeverity.Error)
            .ShouldBeTrue();
}
