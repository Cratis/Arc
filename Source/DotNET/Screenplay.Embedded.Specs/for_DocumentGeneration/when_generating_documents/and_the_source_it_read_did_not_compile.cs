// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

/// <summary>
/// A model recovered from source the C# compiler never accepted describes an application that does not exist, so a
/// poor document made from it is the broken build already reported rather than a second defect. This is the same
/// bargain the generator being reused strikes, and reporting it twice would send a reader looking for a generator
/// bug that is not there.
/// </summary>
public class and_the_source_it_read_did_not_compile : Specification
{
    IScreenplayEmitter _emitter;
    EmbeddedDocumentGeneration _generation;

    void Establish()
    {
        _emitter = Substitute.For<IScreenplayEmitter>();
        _emitter
            .Emit(Arg.Any<Model.ApplicationModel>(), Arg.Any<ScreenplayOptions>())
            .Returns(new ScreenplayEmission("this is not a Screenplay document", null!, []));
    }

    void Because() => _generation = new EmbeddedDocumentGenerator(new ApplicationModelAnalyzer(), _emitter, new ScreenplayVerifier())
        .Generate(
            given.an_application.Build(),
            given.an_application.Options(),
            [new(ScreenplayDiagnosticSeverity.Error, ScreenplayDiagnosticCodes.SourceDidNotCompile, "the source did not compile", null)]);

    [Fact] void should_not_claim_the_generator_produced_a_document_that_does_not_compile() =>
        _generation.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotCompile);

    [Fact] void should_still_report_what_the_source_did() =>
        _generation.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.SourceDidNotCompile);

    [Fact] void should_not_be_successful() => _generation.IsSuccess.ShouldBeFalse();
}
