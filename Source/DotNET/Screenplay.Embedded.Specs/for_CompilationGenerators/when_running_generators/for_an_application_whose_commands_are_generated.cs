// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build.Generators;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.for_CompilationGenerators.when_running_generators;

/// <summary>
/// An Arc application declares a large part of itself through generators, so the documents have to be generated
/// from a compilation that holds the generated source as well as the authored source. Here the command exists
/// nowhere in the project - its name comes from an additional file and its namespace from the analyzer
/// configuration - so a document naming it can only have come from the real generator really running.
/// </summary>
public class for_an_application_whose_commands_are_generated : Specification, IDisposable
{
    readonly given.a_generated_application _application = new();

    Compilation _authored;
    CompilationGeneratorResult _result;
    EmbeddedDocumentGeneration _generation;

    void Establish() => _authored = _application.Compiled();

    void Because()
    {
        _result = CompilationGenerators.Run(
            _authored,
            [_application.Generating],
            [_application.Commands],
            [_application.Configuration]);

        _generation = new EmbeddedDocumentGenerator().Generate(
            _result.Compilation,
            new(given.a_generated_application.AssemblyName, given.a_generated_application.AssemblyName));
    }

    string Document => _generation.Documents.First(_ => _.Document.Id == given.a_generated_application.AssemblyName).Source;

    INamedTypeSymbol? Command =>
        _result.Compilation.GetTypeByMetadataName(
            $"{given.a_generated_application.CommandNamespace}.{given.a_generated_application.CommandName}");

    [Fact] void should_add_the_generated_command_to_the_compilation() => Command.ShouldNotBeNull();

    [Fact] void should_name_it_what_the_additional_file_named() =>
        Command!.Name.ShouldEqual(given.a_generated_application.CommandName);

    [Fact] void should_declare_it_where_the_analyzer_configuration_stated() =>
        Command!.ContainingNamespace.ToDisplayString().ShouldEqual(given.a_generated_application.CommandNamespace);

    [Fact] void should_hand_back_a_compilation_that_holds_the_generated_source() =>
        _result.Compilation.SyntaxTrees.Count().ShouldEqual(_authored.SyntaxTrees.Count() + 1);

    [Fact] void should_leave_the_compilation_it_was_given_alone() =>
        _authored.GetTypeByMetadataName(
            $"{given.a_generated_application.CommandNamespace}.{given.a_generated_application.CommandName}").ShouldBeNull();

    [Fact] void should_hand_back_a_compilation_without_errors() =>
        _result.Compilation.GetDiagnostics().Any(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeFalse();

    [Fact] void should_report_nothing_of_its_own() => _result.Diagnostics.ShouldBeEmpty();

    [Fact] void should_let_the_document_generator_see_the_generated_command() =>
        Document.Contains($"command {given.a_generated_application.CommandName}", StringComparison.Ordinal).ShouldBeTrue();

    [Fact] void should_generate_documents_the_screenplay_compiler_accepts() => _generation.IsSuccess.ShouldBeTrue();

    [Fact] void should_write_nothing_of_the_generated_source_to_disk() =>
        Directory.GetFiles(_application.Directory).Length.ShouldEqual(3);

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _application.Dispose();
    }
}
