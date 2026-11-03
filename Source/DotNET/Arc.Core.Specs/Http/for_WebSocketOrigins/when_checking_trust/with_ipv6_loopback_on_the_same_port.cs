// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_WebSocketOrigins.when_checking_trust;

public class with_ipv6_loopback_on_the_same_port : Specification
{
    bool _result;

    void Because() => _result = WebSocketOrigins.IsTrusted("http://[::1]:5001", "[::1]:5001", false, []);

    [Fact] void should_be_trusted() => _result.ShouldEqual(true);
}
