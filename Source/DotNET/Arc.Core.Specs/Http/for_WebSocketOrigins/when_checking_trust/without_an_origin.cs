// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_WebSocketOrigins.when_checking_trust;

public class without_an_origin : Specification
{
    bool _result;

    void Because() => _result = WebSocketOrigins.IsTrusted(null, "app.test", false, []);

    [Fact] void should_be_trusted() => _result.ShouldEqual(true);
}
