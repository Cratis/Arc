// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class with_generated_registrations_arriving_later : given.a_generated_metadata_registration
{
    int _firstRuns;
    int _secondRuns;

    void Establish()
    {
        _registration.Register(() => _firstRuns++, []);
        _registration.EnsureRegistered();
        _registration.Register(() => _secondRuns++, []);
    }

    void Because() => _registration.EnsureRegistered();

    [Fact] void should_not_run_the_earlier_registration_again() => _firstRuns.ShouldEqual(1);
    [Fact] void should_run_the_later_registration() => _secondRuns.ShouldEqual(1);
}
