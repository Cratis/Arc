// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class with_generated_registrations_from_another_executable : given.a_generated_metadata_registration
{
    int _moduleLookups;

    void Establish()
    {
        _getDependencyContextProjectNames().Returns(["Test.Only.Project"]);
        _registration.Register(
            _otherExecutable,
            Modules(("Web.Project", GetModule)),
            []);
    }

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

    [Fact] void should_reach_the_module_of_the_registered_project_reference() => _moduleLookups.ShouldEqual(1);
    [Fact] void should_consult_the_dependency_context_once() => _getDependencyContextProjectNames.Received(1)();
    [Fact] void should_load_the_project_libraries_of_the_dependency_context() => _loadAssembly.Received(1)(Arg.Is<AssemblyName>(_ => _.Name == "Test.Only.Project"));
}
