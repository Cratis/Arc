// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_WebSocketOrigins.when_checking_trust;

public class with_a_configured_origin : Specification
{
    bool _result;

    void Because() => _result = WebSocketOrigins.IsTrusted("HTTPS://Other.Test", "app.test", true, ["https://other.test/"]);

    [Fact] void should_be_trusted() => _result.ShouldEqual(true);
}
