// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class with_generated_registrations : given.a_generated_metadata_registration
{
    int _moduleInitializerRuns;

    void Establish() => _registration.Register(() => _moduleInitializerRuns++, ["Unnamed.Project"]);

    void Because()
    {
        _registration.EnsureRegistered();
        _registration.EnsureRegistered();
    }

    [Fact] void should_run_the_generated_module_initializers_once() => _moduleInitializerRuns.ShouldEqual(1);
    [Fact] void should_load_the_project_reference_generated_code_could_not_name_once() => _loadAssembly.Received(1)(Arg.Is<AssemblyName>(_ => _.Name == "Unnamed.Project"));
    [Fact] void should_not_consult_the_dependency_context() => _getDependencyContextProjectNames.DidNotReceive()();
}
