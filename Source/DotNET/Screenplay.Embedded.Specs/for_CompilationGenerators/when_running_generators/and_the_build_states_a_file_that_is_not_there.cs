// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build.Generators;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.for_CompilationGenerators.when_running_generators;

/// <summary>
/// Every file here was stated by the build because the C# compiler is about to be given it. Skipping one that is
/// not on disk would run a different set of generators, or run them against different inputs, than the compiler
/// will - and the difference would show up as documents missing part of the application rather than as a failure.
/// </summary>
public class and_the_build_states_a_file_that_is_not_there : Specification, IDisposable
{
    readonly given.a_generated_application _application = new();

    Compilation _authored;
    Exception _analyzer;
    Exception _additional;
    Exception _configuration;

    void Establish() => _authored = _application.Compiled();

    void Because()
    {
        _analyzer = Catch.Exception(() => CompilationGenerators.Run(
            _authored,
            [_application.Missing("Absent.Generator.dll")],
            [],
            []));

        _additional = Catch.Exception(() => CompilationGenerators.Run(
            _authored,
            [_application.Generating],
            [_application.Missing("absent.txt")],
            [_application.Configuration]));

        _configuration = Catch.Exception(() => CompilationGenerators.Run(
            _authored,
            [_application.Generating],
            [_application.Commands],
            [_application.Missing("absent.globalconfig")]));
    }

    [Fact] void should_fail_on_an_analyzer_that_is_not_there() => _analyzer.ShouldBeOfExactType<AnalyzerNotFound>();

    [Fact] void should_fail_on_an_additional_file_that_is_not_there() => _additional.ShouldBeOfExactType<AdditionalFileNotFound>();

    [Fact] void should_fail_on_a_configuration_file_that_is_not_there() => _configuration.ShouldBeOfExactType<AnalyzerConfigurationNotFound>();

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _application.Dispose();
    }
}
