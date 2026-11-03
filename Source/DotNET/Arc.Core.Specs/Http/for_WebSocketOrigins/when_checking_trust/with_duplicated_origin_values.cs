// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_WebSocketOrigins.when_checking_trust;

public class with_duplicated_origin_values : Specification
{
    bool _result;

    void Because() => _result = WebSocketOrigins.IsTrusted("https://app.test, https://app.test", "app.test", true, []);

    [Fact] void should_be_untrusted() => _result.ShouldEqual(false);
}
