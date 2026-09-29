// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_generating_with_csharp_10 : given.project_libraries
{
    ProjectReferenceModulesGeneratorResult _result;

    void Because() => _result = ProjectReferenceModulesGeneratorRunner.Run(
        OutputKind.ConsoleApplication,
        "2|VisibleLibrary|HiddenLibrary",
        LanguageVersion.CSharp10,
        [_visibleLibrary.Reference, _hiddenLibrary.Reference]);

    [Fact] void should_generate_the_registration() => _result.Source.ShouldNotBeNull();
    [Fact] void should_leave_the_compilation_valid() => _result.Errors.ShouldBeEmpty();
}
