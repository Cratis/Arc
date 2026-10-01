// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_ArcOptions;

public class when_creating_with_defaults : Specification
{
    ArcOptions _options;

    void Because() => _options = new ArcOptions();

    [Fact] void should_not_trust_forwarded_identity_headers() => _options.TrustForwardedIdentityHeaders.ShouldBeFalse();
}
