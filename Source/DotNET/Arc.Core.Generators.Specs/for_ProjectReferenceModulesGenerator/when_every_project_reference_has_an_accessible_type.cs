// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_every_project_reference_has_an_accessible_type : given.project_libraries
{
    ProjectReferenceModulesGeneratorResult _result;

    void Because() => _result = ProjectReferenceModulesGeneratorRunner.Run(OutputKind.ConsoleApplication, "1|VisibleLibrary", _visibleLibrary);

    [Fact] void should_not_leave_any_assembly_to_load_by_name() => _result.Source!.Contains("            global::System.Array.Empty<string>());", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_the_compilation_valid() => _result.Errors.ShouldBeEmpty();
}
