// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class and_a_module_initializer_registers_again : given.a_generated_metadata_registration
{
    int _innerLookups;
    Exception _error;

    void Establish()
    {
        // Running the module initializer of a project reference can reach an executable whose own generated module
        // initializer registers, and a registration can ensure registration again from within.
        _registration.Register(_entryAssembly, Modules((_initializedAssemblyName, GetOuterModule)), []);
    }

    void Because() => _error = Catch.Exception(_registration.EnsureRegistered);

    Module GetOuterModule()
    {
        _registration.Register(_entryAssembly, Modules((_initializedAssemblyName, GetInnerModule)), []);
        _registration.EnsureRegistered();
        return _initializedModule;
    }

    Module GetInnerModule()
    {
        _innerLookups++;
        return _initializedModule;
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_run_the_registration_made_from_within_once() => _innerLookups.ShouldEqual(1);
}
