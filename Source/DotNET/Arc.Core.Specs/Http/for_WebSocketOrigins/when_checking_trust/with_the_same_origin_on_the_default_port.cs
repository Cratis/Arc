// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_WebSocketOrigins.when_checking_trust;

public class with_the_same_origin_on_the_default_port : Specification
{
    bool _result;

    void Because() => _result = WebSocketOrigins.IsTrusted("https://app.test", "app.test", true, []);

    [Fact] void should_be_trusted() => _result.ShouldEqual(true);
}
