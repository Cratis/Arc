// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build.Generators;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.for_CompilationGenerators.when_running_generators;

/// <summary>
/// Roslyn reports an analyzer it cannot read and carries on with the rest, which is how a build quietly compiles
/// an application whose generated half never existed. A file the build called an analyzer and which turned out not
/// to be one ends the run here, because what it would have generated is unknowable.
/// </summary>
public class and_an_analyzer_is_not_an_assembly : Specification, IDisposable
{
    readonly given.a_generated_application _application = new();

    Compilation _authored;
    Exception _exception;

    void Establish() => _authored = _application.Compiled();

    void Because() => _exception = Catch.Exception(() =>
        CompilationGenerators.Run(_authored, [_application.NotAnAssembly, _application.Generating], [_application.Commands], [_application.Configuration]));

    [Fact] void should_fail() => _exception.ShouldBeOfExactType<GeneratorsCouldNotBeLoaded>();

    [Fact] void should_name_the_analyzer_it_could_not_read() =>
        _exception.Message.Contains(_application.NotAnAssembly, StringComparison.Ordinal).ShouldBeTrue();

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _application.Dispose();
    }
}
