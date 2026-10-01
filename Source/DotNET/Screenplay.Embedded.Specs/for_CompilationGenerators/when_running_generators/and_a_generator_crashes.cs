// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build.Generators;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.for_CompilationGenerators.when_running_generators;

/// <summary>
/// The driver answers a crashed generator with a compilation that simply lacks whatever that generator would have
/// added, and a diagnostic beside it. Carrying on would embed documents describing an application missing the part
/// nobody can see is missing, so the run ends instead.
/// </summary>
public class and_a_generator_crashes : Specification, IDisposable
{
    readonly given.a_generated_application _application = new();

    Compilation _authored;
    Exception _exception;

    void Establish() => _authored = _application.Compiled();

    void Because() => _exception = Catch.Exception(() =>
        CompilationGenerators.Run(_authored, [_application.Crashing], [], []));

    [Fact] void should_fail() => _exception.ShouldBeOfExactType<GeneratorFailed>();

    [Fact] void should_say_what_the_generator_said() =>
        _exception.Message.Contains(given.the_analyzers_of_a_project.Crash, StringComparison.Ordinal).ShouldBeTrue();

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _application.Dispose();
    }
}
