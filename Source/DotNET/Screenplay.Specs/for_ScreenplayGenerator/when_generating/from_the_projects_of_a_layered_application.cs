// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Screenplay;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The whole promise again, for the applications that are not one project. Two compilations go in - a contracts
/// project and the project handling the commands its events belong to - and one document comes out that the
/// Screenplay compiler accepts without a diagnostic. Pointed at either project alone, the generator described half
/// of this and referred to an event it never introduced.
/// </summary>
public class from_the_projects_of_a_layered_application : Specification
{
    Compilation _contracts;
    Compilation _application;
    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;

    void Establish()
    {
        _contracts = LayeredSource.ContractsProject();
        _application = LayeredSource.ApplicationProject(_contracts);
    }

    void Because()
    {
        _result = new ScreenplayGenerator(
                new ApplicationModelAnalyzer(DeclaredUserInterfaceFiles.None),
                new ScreenplayEmitter())
            .Generate([_application, _contracts], new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
    }

    bool Says(string text) => _result.Source.Contains(text, StringComparison.Ordinal);

    int Counted(string text) => _result.Source.Split(text, StringSplitOptions.None).Length - 1;

    [Fact] void should_compile_the_contracts_project() => Analyzed.ErrorsIn(_contracts).ShouldBeEmpty();
    [Fact] void should_compile_the_application_project() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_declare_the_command_the_application_project_holds() => Says("command PlaceOrder").ShouldBeTrue();
    [Fact] void should_declare_the_event_the_contracts_project_holds() => Says("event OrderPlaced").ShouldBeTrue();
    [Fact] void should_state_what_the_command_produces() => Says("produces OrderPlaced").ShouldBeTrue();
    [Fact] void should_declare_the_slice_only_the_application_project_holds() => Says("query AllOrders").ShouldBeTrue();
    [Fact] void should_observe_the_event_of_one_project_from_a_reactor_in_the_other() => Says("reactor Dispatcher").ShouldBeTrue();
    [Fact] void should_declare_a_concept_both_projects_refer_to_once() => Counted("concept CustomerName : String @pii").ShouldEqual(1);
    [Fact] void should_write_the_path_of_a_file_relative_to_the_directory_the_projects_share() => Says("Library/Shipping/Dispatching/Dispatching.cs").ShouldBeTrue();
    [Fact] void should_import_nothing() => _result.Model.Imports.ShouldBeEmpty();
    [Fact] void should_not_refer_to_an_event_it_never_introduces() => _result.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.EventDeclaredOutsideCompilation).ShouldBeFalse();
    [Fact] void should_report_nothing_as_unmappable() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
