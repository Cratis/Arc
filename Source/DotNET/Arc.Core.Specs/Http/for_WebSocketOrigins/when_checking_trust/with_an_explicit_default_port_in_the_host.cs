// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_WebSocketOrigins.when_checking_trust;

public class with_an_explicit_default_port_in_the_host : Specification
{
    bool _result;

    void Because() => _result = WebSocketOrigins.IsTrusted("https://app.test", "app.test:443", true, []);

    [Fact] void should_be_trusted() => _result.ShouldEqual(true);
}
