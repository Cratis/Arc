// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_generating_for_a_library : given.project_libraries
{
    ProjectReferenceModulesGeneratorResult _result;

    void Because() => _result = ProjectReferenceModulesGeneratorRunner.Run(OutputKind.DynamicallyLinkedLibrary, "1|VisibleLibrary", _visibleLibrary);

    [Fact] void should_not_generate_anything() => _result.Source.ShouldBeNull();
}
