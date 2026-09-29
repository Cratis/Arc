// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class with_generated_registrations_arriving_later : given.a_generated_metadata_registration
{
    int _firstLookups;
    int _secondLookups;

    void Establish()
    {
        _registration.Register(_entryAssembly, Modules((_initializedAssemblyName, GetFirstModule)), []);
        _registration.EnsureRegistered();
        _registration.Register(_entryAssembly, Modules((_initializedAssemblyName, GetSecondModule)), []);
    }

    void Because() => _registration.EnsureRegistered();

    Module GetFirstModule()
    {
        _firstLookups++;
        return _initializedModule;
    }

    Module GetSecondModule()
    {
        _secondLookups++;
        return _initializedModule;
    }

    [Fact] void should_not_run_the_earlier_registration_again() => _firstLookups.ShouldEqual(1);
    [Fact] void should_run_the_later_registration() => _secondLookups.ShouldEqual(1);
}
