// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class without_generated_registrations : given.a_generated_metadata_registration
{
    void Establish() => _getDependencyContextProjectNames().Returns(["First.Project", "Second.Project"]);

    void Because()
    {
        _registration.EnsureRegistered();
        _registration.EnsureRegistered();
    }

    [Fact] void should_consult_the_dependency_context_once() => _getDependencyContextProjectNames.Received(1)();
    [Fact] void should_load_the_first_project_library() => _loadAssembly.Received(1)(Arg.Is<AssemblyName>(_ => _.Name == "First.Project"));
    [Fact] void should_load_the_second_project_library() => _loadAssembly.Received(1)(Arg.Is<AssemblyName>(_ => _.Name == "Second.Project"));
    [Fact] void should_not_report_anything_as_skipped() => _registration.Skipped.ShouldBeEmpty();
}
