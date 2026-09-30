// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_the_project_references_are_cut_short : given.project_libraries
{
    ProjectReferenceModulesGeneratorResult _result;

    void Because() => _result = ProjectReferenceModulesGeneratorRunner.Run(OutputKind.ConsoleApplication, "3|VisibleLibrary|HiddenLibrary", _visibleLibrary, _hiddenLibrary);

    [Fact] void should_not_generate_anything_so_the_runtime_falls_back_to_the_dependency_context() => _result.Source.ShouldBeNull();
}
