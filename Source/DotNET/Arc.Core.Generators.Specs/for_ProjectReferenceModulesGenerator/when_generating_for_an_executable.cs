// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_generating_for_an_executable : given.project_libraries
{
    ProjectReferenceModulesGeneratorResult _result;

    void Because() => _result = ProjectReferenceModulesGeneratorRunner.Run(
        OutputKind.ConsoleApplication,
        "2|VisibleLibrary|HiddenLibrary",
        _visibleLibrary,
        _hiddenLibrary,
        _packageLibrary);

    [Fact] void should_reach_the_module_of_a_project_reference_through_a_type_in_it_in_its_own_lambda() => _result.Source!.Contains("[\"VisibleLibrary\"] = static () => typeof(global::VisibleLibrary.Things.Visible).Module,", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_register_from_a_module_initializer() => _result.Source!.Contains("[global::System.Runtime.CompilerServices.ModuleInitializer]", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_register_with_the_executable_assembly() => _result.Source!.Contains("typeof(__CratisArcProjectReferenceModules).Assembly,", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_load_a_project_reference_without_accessible_types_by_name() => _result.Source!.Contains("new string[] { \"HiddenLibrary\" });", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_name_a_type_in_a_project_reference_without_accessible_types() => _result.Source!.Contains("global::HiddenLibrary", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_leave_references_that_are_not_project_references_alone() => _result.Source!.Contains("PackageLibrary", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_use_a_file_local_type() => _result.Source!.Contains("file ", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_leave_the_compilation_valid() => _result.Errors.ShouldBeEmpty();
}
