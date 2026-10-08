// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

/// <summary>
/// What an assembly embeds is a document per part of the application, each one a document in its own right. A
/// scoped document that only compiles because the rest of the application happened to be in the same file is one
/// nobody can open, so every one of them is read back with the compiler the language ships - here, and in the
/// generator itself.
/// </summary>
public class for_an_application : Specification
{
    EmbeddedDocumentGeneration _generation;

    void Because() => _generation = new EmbeddedDocumentGenerator()
        .Generate(given.an_application.Build(), given.an_application.Options(), []);

    GeneratedDocument DocumentOf(string id) => _generation.Documents.First(_ => _.Document.Id == id);

    [Fact] void should_generate_a_document_per_part_of_the_application() => _generation.Documents.Count.ShouldEqual(5);

    [Fact] void should_succeed() => _generation.IsSuccess.ShouldBeTrue();

    [Fact] void should_report_only_binding_defects() => _generation.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldEqual([ScreenplayDiagnosticCodes.DocumentDidNotBind]);

    [Fact] void should_report_the_known_binding_defects_without_accepting_new_ones() => _generation.Diagnostics.Count.ShouldEqual(10);

    [Fact] void should_generate_documents_the_screenplay_compiler_accepts() =>
        _generation.Documents.All(_ => new ScreenplayCompiler().Compile(_.Source).Success).ShouldBeTrue();

    [Fact] void should_hold_the_whole_application_in_the_assembly_document() =>
        DocumentOf(given.an_application.RootNamespace).Source.Contains("slice StateChange Housekeeping", StringComparison.Ordinal).ShouldBeTrue();

    [Fact] void should_declare_the_module_as_a_module_of_the_assembly_document() =>
        DocumentOf(given.an_application.RootNamespace).Source.Contains("module Accounting", StringComparison.Ordinal).ShouldBeTrue();

    [Fact] void should_declare_the_module_document_within_the_module() =>
        DocumentOf(given.an_application.Module).Source.Contains("module Accounting", StringComparison.Ordinal).ShouldBeTrue();

    [Fact] void should_hold_only_the_module_in_the_module_document() =>
        DocumentOf(given.an_application.Module).Source.Contains("Housekeeping", StringComparison.Ordinal).ShouldBeFalse();

    [Fact] void should_hold_the_feature_within_the_module_document() =>
        DocumentOf(given.an_application.Module).Source.Contains("feature Invoices", StringComparison.Ordinal).ShouldBeTrue();

    [Fact] void should_hold_the_nested_feature_within_the_feature_document() =>
        DocumentOf(given.an_application.Feature).Source.Contains("feature Payments", StringComparison.Ordinal).ShouldBeTrue();

    [Fact] void should_hold_only_the_feature_in_the_feature_document() =>
        DocumentOf(given.an_application.Feature).Source.Contains("Reporting", StringComparison.Ordinal).ShouldBeFalse();

    [Fact] void should_hold_only_the_rooted_feature_in_its_document() =>
        DocumentOf(given.an_application.RootedFeature).Source.Contains("Invoices", StringComparison.Ordinal).ShouldBeFalse();

    [Fact] void should_declare_the_rooted_feature_as_a_feature_rather_than_as_a_module() =>
        DocumentOf(given.an_application.RootedFeature).Source.Contains("feature Authors", StringComparison.Ordinal).ShouldBeTrue();

    [Fact] void should_import_an_event_another_scope_declares() =>
        DocumentOf(given.an_application.NestedFeature).Source
            .Contains($"import Library.Authors.Registration.{given.an_application.EventOfTheRootedFeature}", StringComparison.Ordinal)
            .ShouldBeTrue();

    [Fact] void should_import_an_event_a_sibling_feature_of_the_same_module_declares() =>
        DocumentOf(given.an_application.NestedFeature).Source
            .Contains($"import Library.Accounting.Invoices.Issuing.{given.an_application.EventOfTheFeature}", StringComparison.Ordinal)
            .ShouldBeTrue();

    [Fact] void should_not_import_an_event_the_scope_declares_itself() =>
        DocumentOf(given.an_application.Feature).Source
            .Contains($"import Library.Accounting.Invoices.Issuing.{given.an_application.EventOfTheFeature}", StringComparison.Ordinal)
            .ShouldBeFalse();

    [Fact] void should_import_nothing_into_the_document_holding_the_whole_application() =>
        DocumentOf(given.an_application.RootNamespace).Source.Contains("import ", StringComparison.Ordinal).ShouldBeFalse();

    [Fact] void should_generate_the_same_documents_every_time() =>
        new EmbeddedDocumentGenerator()
            .Generate(given.an_application.Build(), given.an_application.Options(), [])
            .Documents
            .SequenceEqual(_generation.Documents)
            .ShouldBeTrue();
}
