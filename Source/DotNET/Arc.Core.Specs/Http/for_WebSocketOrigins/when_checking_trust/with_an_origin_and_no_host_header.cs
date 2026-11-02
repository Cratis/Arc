// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_WebSocketOrigins.when_checking_trust;

public class with_an_origin_and_no_host_header : Specification
{
    bool _result;

    void Because() => _result = WebSocketOrigins.IsTrusted("https://app.test", null, true, []);

    [Fact] void should_be_untrusted() => _result.ShouldEqual(false);
}
