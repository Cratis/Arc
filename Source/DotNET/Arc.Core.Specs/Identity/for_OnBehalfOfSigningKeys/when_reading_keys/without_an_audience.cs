// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_OnBehalfOfSigningKeys.when_reading_keys;

public class without_an_audience : Specification
{
    OnBehalfOfOptions _options;
    OnBehalfOfSigningKeys _keys;

    void Establish()
    {
        _options = OnBehalfOfAssertionsForSpecs.Options();
        _options.Audience = string.Empty;
    }

    void Because() => _keys = OnBehalfOfSigningKeys.For(_options);

    [Fact] void should_refuse_the_configuration() => _keys.Problem.ShouldNotBeNull();
    [Fact] void should_say_why() => _keys.Problem.ShouldContain("an issuer and an audience");
}
