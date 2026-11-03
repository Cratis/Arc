// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_WebSocketOrigins.when_checking_trust;

public class with_another_localhost_port : Specification
{
    bool _result;

    void Because() => _result = WebSocketOrigins.IsTrusted("http://localhost:9000", "localhost:5001", false, []);

    [Fact] void should_be_untrusted() => _result.ShouldEqual(false);
}
