// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_reading_unchanged_configuration : Specification
{
    ArcOptions _options;
    ForwardedIdentityConfiguration _first;
    ForwardedIdentityConfiguration _second;

    void Establish() => _options = ForwardedIdentityForSpecs.Options();

    void Because()
    {
        _first = ForwardedIdentityConfiguration.Read(_options);
        _second = ForwardedIdentityConfiguration.Read(_options);
    }

    [Fact] void should_read_a_valid_configuration() => _first.ShouldNotBeNull();
    [Fact] void should_return_the_same_configuration() => _second.ShouldBeSame(_first);
}
