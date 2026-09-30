// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_a_project_reference_has_a_file_local_type : given.project_libraries
{
    ProjectReferenceModulesGeneratorResult _result;

    void Because() => _result = ProjectReferenceModulesGeneratorRunner.Run(OutputKind.ConsoleApplication, "1|FileLocalLibrary", _fileLocalLibrary);

    [Fact] void should_leave_the_compilation_valid() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_not_name_a_file_local_type() => _result.Source!.Contains("Registration)", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_name_the_public_type() => _result.Source!.Contains("typeof(global::FileLocalLibrary.Visible).Module", StringComparison.Ordinal).ShouldBeTrue();
}
