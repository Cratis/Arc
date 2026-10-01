// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build.Generators;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.for_CompilationGenerators.when_running_generators;

/// <summary>
/// Most analyzer packages only analyze. A project referencing one of those, or no analyzer at all, is an ordinary
/// project and not a failure - it simply has nothing generated to add to what its source already says.
/// </summary>
public class and_the_analyzers_generate_nothing : Specification, IDisposable
{
    readonly given.a_generated_application _application = new();

    Compilation _authored;
    CompilationGeneratorResult _analyzerOnly;
    CompilationGeneratorResult _withoutAnalyzers;

    void Establish() => _authored = _application.Compiled();

    void Because()
    {
        _analyzerOnly = CompilationGenerators.Run(_authored, [_application.Analyzing], [], []);
        _withoutAnalyzers = CompilationGenerators.Run(_authored, [], [], []);
    }

    [Fact] void should_hand_back_the_compilation_it_was_given_for_an_analyzer_that_generates_nothing() =>
        _analyzerOnly.Compilation.SyntaxTrees.Count().ShouldEqual(_authored.SyntaxTrees.Count());

    [Fact] void should_report_nothing_for_an_analyzer_that_generates_nothing() => _analyzerOnly.Diagnostics.ShouldBeEmpty();

    [Fact] void should_hand_back_the_compilation_it_was_given_for_a_project_with_no_analyzers() =>
        _withoutAnalyzers.Compilation.ShouldEqual(_authored);

    [Fact] void should_report_nothing_for_a_project_with_no_analyzers() => _withoutAnalyzers.Diagnostics.ShouldBeEmpty();

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _application.Dispose();
    }
}
