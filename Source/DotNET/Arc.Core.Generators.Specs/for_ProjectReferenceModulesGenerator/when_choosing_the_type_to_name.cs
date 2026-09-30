// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_choosing_the_type_to_name : given.project_libraries
{
    ProjectReferenceModulesGeneratorResult _result;

    void Because() => _result = ProjectReferenceModulesGeneratorRunner.Run(OutputKind.ConsoleApplication, "2|ChoosingLibrary|DerivedLibrary", _choosingLibrary, _derivedLibrary);

    [Fact] void should_prefer_a_type_without_interfaces_or_a_base_type_outside_the_core_library() => _result.Source!.Contains("typeof(global::ChoosingLibrary.WithoutDependencies).Module", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_name_a_type_implementing_an_interface_when_there_is_another() => _result.Source!.Contains("global::ChoosingLibrary.AImplementsAnInterface", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_name_a_type_with_dependencies_when_there_is_no_other() => _result.Source!.Contains("typeof(global::DerivedLibrary.Derived).Module", StringComparison.Ordinal).ShouldBeTrue();
}
