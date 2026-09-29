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

    [Fact] void should_run_the_module_initializer_of_a_project_reference_through_a_type_in_it() => _result.Source!.Contains("RunModuleConstructor(typeof(global::VisibleLibrary.Things.Visible).Module.ModuleHandle);", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_register_the_module_initializers_from_a_module_initializer() => _result.Source!.Contains("global::Cratis.Arc.ProjectReferenceModuleInitializers.Register(RunModuleInitializers, new string[] { \"HiddenLibrary\" });", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_name_a_type_in_a_project_reference_without_accessible_types() => _result.Source!.Contains("global::HiddenLibrary", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_leave_references_that_are_not_project_references_alone() => _result.Source!.Contains("PackageLibrary", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_leave_the_compilation_valid() => _result.Errors.ShouldBeEmpty();
}
