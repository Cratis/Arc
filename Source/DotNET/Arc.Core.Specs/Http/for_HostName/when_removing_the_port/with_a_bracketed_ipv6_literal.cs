// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HostName.when_removing_the_port;

public class with_a_bracketed_ipv6_literal : Specification
{
    string _result;

    void Because() => _result = HostName.WithoutPort("[::1]");

    [Fact] void should_keep_the_literal_unchanged() => _result.ShouldEqual("[::1]");
}
