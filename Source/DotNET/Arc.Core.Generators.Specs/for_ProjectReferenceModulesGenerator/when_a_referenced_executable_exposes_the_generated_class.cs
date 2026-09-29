// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_a_referenced_executable_exposes_the_generated_class : given.project_libraries
{
    ProjectReferenceModulesGeneratorResult _result;

    void Because() => _result = ProjectReferenceModulesGeneratorRunner.Run(OutputKind.ConsoleApplication, "1|ExecutableLibrary", _executableLibrary);

    [Fact] void should_not_fail() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_not_name_the_generated_registration_class_of_the_referenced_executable() => _result.Source!.Contains("typeof(global::Cratis.Arc.Generated.__CratisArcProjectReferenceModules)", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_name_any_other_type_in_a_generated_namespace() => _result.Source!.Contains("typeof(global::Cratis.Arc.Commands.Generated.OperationRegistration)", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_load_the_project_reference_by_name_instead() => _result.Source!.Contains("new string[] { \"ExecutableLibrary\" }", StringComparison.Ordinal).ShouldBeTrue();
}
