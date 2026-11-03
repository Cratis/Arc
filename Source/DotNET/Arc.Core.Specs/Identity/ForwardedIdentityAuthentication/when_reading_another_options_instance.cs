// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_reading_another_options_instance : Specification
{
    ForwardedIdentityConfiguration _first;
    ForwardedIdentityConfiguration _second;

    void Because()
    {
        _first = ForwardedIdentityConfiguration.Read(ForwardedIdentityForSpecs.Options());
        _second = ForwardedIdentityConfiguration.Read(ForwardedIdentityForSpecs.Options());
    }

    [Fact] void should_not_share_configuration() => _second.ShouldNotBeSame(_first);
    [Fact] void should_read_equivalent_keys() => _second.Snapshot.ShouldEqual(_first.Snapshot);
}
