// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ProjectReferenceModulesGenerator;

public class when_a_reported_project_reference_is_not_referenced : given.project_libraries
{
    ProjectReferenceModulesGeneratorResult _result;

    void Because() => _result = ProjectReferenceModulesGeneratorRunner.Run(OutputKind.ConsoleApplication, "2|VisibleLibrary|Unresolved.Library", _visibleLibrary);

    [Fact] void should_load_it_by_name_at_runtime() => _result.Source!.Contains("new string[] { \"Unresolved.Library\" });", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_reach_the_resolved_project_reference() => _result.Source!.Contains("typeof(global::VisibleLibrary.Things.Visible).Module", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_the_compilation_valid() => _result.Errors.ShouldBeEmpty();
}
