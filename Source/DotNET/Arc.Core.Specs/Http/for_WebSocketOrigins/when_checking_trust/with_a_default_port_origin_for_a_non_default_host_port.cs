// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_WebSocketOrigins.when_checking_trust;

public class with_a_default_port_origin_for_a_non_default_host_port : Specification
{
    bool _result;

    void Because() => _result = WebSocketOrigins.IsTrusted("http://app.test", "app.test:5000", false, []);

    [Fact] void should_be_untrusted() => _result.ShouldEqual(false);
}
