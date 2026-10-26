// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_ArcOptions;

public class when_constructing_without_on_behalf_of_configuration : Specification
{
    ArcOptions _options;

    void Because() => _options = new ArcOptions();

    [Fact] void should_not_accept_on_behalf_forwards() => _options.OnBehalfOf.Enabled.ShouldBeFalse();
    [Fact] void should_have_no_signing_keys() => _options.OnBehalfOf.SigningKeys.ShouldBeEmpty();
}
