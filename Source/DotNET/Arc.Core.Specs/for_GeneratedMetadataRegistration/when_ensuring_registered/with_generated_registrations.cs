// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class with_generated_registrations : given.a_generated_metadata_registration
{
    int _moduleLookups;

    void Establish() => _registration.Register(
        _entryAssembly,
        Modules((_initializedAssemblyName, GetModule)),
        ["Unnamed.Project"]);

    void Because()
    {
        _registration.EnsureRegistered();
        _registration.EnsureRegistered();
    }

    Module GetModule()
    {
        _moduleLookups++;
        return _initializedModule;
    }

    [Fact] void should_reach_the_module_of_the_named_project_reference_once() => _moduleLookups.ShouldEqual(1);
    [Fact] void should_not_load_the_named_project_reference_by_name() => _loadAssembly.DidNotReceive()(Arg.Is<AssemblyName>(_ => _.Name == _initializedAssemblyName));
    [Fact] void should_load_the_project_reference_generated_code_could_not_name_once() => _loadAssembly.Received(1)(Arg.Is<AssemblyName>(_ => _.Name == "Unnamed.Project"));
    [Fact] void should_not_consult_the_dependency_context_when_the_entry_assembly_registered() => _getDependencyContextProjectNames.DidNotReceive()();
}
