// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

public class and_the_whole_application_has_a_malformed_condition : Specification
{
    const string Source = "concept Role : Enum\n" +
        "  reader\n" +
        "  administrator\n" +
        "module Library\n" +
        "  feature Authors\n" +
        "    slice StateChange Registration\n" +
        "      command Register\n" +
        "        id Uuid identifier\n" +
        "        role Role\n" +
        "        produces when role == 2\n" +
        "          Registered\n" +
        "            for id\n" +
        "            role = role\n" +
        "      event Registered\n" +
        "        role Role";

    EmbeddedDocumentGeneration _generation;

    void Because()
    {
        var emitter = Substitute.For<IScreenplayEmitter>();
        emitter.Emit(Arg.Any<Model.ApplicationModel>(), Arg.Any<ScreenplayOptions>())
            .Returns(new ScreenplayEmission(Source, null!, []));
        _generation = new EmbeddedDocumentGenerator(new ApplicationModelAnalyzer(), emitter, new ScreenplayVerifier())
            .Generate(given.an_application.Build(), given.an_application.Options(), []);
    }

    [Fact] void should_report_the_whole_application_defect() => _generation.Diagnostics.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.DocumentDidNotBind);
    [Fact] void should_identify_the_whole_application_document() => _generation.Diagnostics.Single().Location.ShouldEqual(given.an_application.RootNamespace);
    [Fact] void should_preserve_the_binder_code() => _generation.Diagnostics.Single().Message.ShouldContain("PLAY0268");
    [Fact] void should_preserve_the_operand_mismatch() => _generation.Diagnostics.Single().Message.ShouldContain("Condition operand for 'role' must match its scalar type and declared enumeration values.");
    [Fact] void should_not_break_consumer_builds() => _generation.Diagnostics.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_preserve_every_document() => _generation.Documents.Count.ShouldEqual(5);
}
