// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class and_a_module_resolves_to_another_assembly : given.a_generated_metadata_registration
{
    Exception _error;

    void Establish()
    {
        // The type named for the project reference is shadowed by one in the executable, so the module reached is not
        // the module of the project reference.
        _registration.Register(_entryAssembly, Modules(("Shadowed.Project", () => _initializedModule)), []);
    }

    void Because() => _error = Catch.Exception(_registration.EnsureRegistered);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_load_the_project_reference_by_name_instead() => _loadAssembly.Received(1)(Arg.Is<AssemblyName>(_ => _.Name == "Shadowed.Project"));
}
