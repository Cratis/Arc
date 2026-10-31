// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityOptions;

public class when_created : Specification
{
    ArcOptions _options;

    void Because() => _options = new();

    [Fact] void should_disable_signed_forwarding_by_default() => _options.ForwardedIdentity.Enabled.ShouldBeFalse();
    [Fact] void should_not_supply_a_deployable_audience() => _options.ForwardedIdentity.Audience.ShouldEqual(string.Empty);
    [Fact] void should_not_supply_deployable_keys() => _options.ForwardedIdentity.Keys.ShouldBeEmpty();
    [Fact] void should_preserve_the_existing_distrust_default() => _options.TrustForwardedIdentityHeaders.ShouldBeFalse();
}
