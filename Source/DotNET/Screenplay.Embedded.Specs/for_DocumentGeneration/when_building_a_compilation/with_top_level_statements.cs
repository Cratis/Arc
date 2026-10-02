// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_building_a_compilation;

public class with_top_level_statements : Specification
{
    DirectoryInfo _directory;
    string _source;
    Compilation _compilation;

    void Establish()
    {
        _directory = Directory.CreateTempSubdirectory();
        _source = Path.Combine(_directory.FullName, "Program.cs");
        File.WriteAllText(_source, "System.Console.WriteLine(\"An executable application\");");
    }

    void Because() => _compilation = SourceCompilation.Create(
        "ExecutableApplication",
        [_source],
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator),
        null,
        "latest",
        "Exe");

    void Destroy() => _directory?.Delete(recursive: true);

    [Fact] void should_accept_the_same_top_level_statements_as_the_actual_build() => _compilation.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_create_an_executable_compilation() => _compilation.Options.OutputKind.ShouldEqual(OutputKind.ConsoleApplication);
}
