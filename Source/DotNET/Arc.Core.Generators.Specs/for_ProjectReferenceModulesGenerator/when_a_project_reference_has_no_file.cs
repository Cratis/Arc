// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_a_project_reference_has_no_file : given.project_libraries
{
    ProjectReferenceModulesGeneratorResult _result;

    void Because() => _result = ProjectReferenceModulesGeneratorRunner.Run(
        OutputKind.ConsoleApplication,
        "1|VisibleLibrary",
        LanguageVersion.Latest,
        [_visibleLibrary.ReferenceWithoutFile]);

    [Fact] void should_match_it_by_assembly_name() => _result.Source!.Contains("[\"VisibleLibrary\"] = static () => typeof(global::VisibleLibrary.Things.Visible).Module,", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_the_compilation_valid() => _result.Errors.ShouldBeEmpty();
}
