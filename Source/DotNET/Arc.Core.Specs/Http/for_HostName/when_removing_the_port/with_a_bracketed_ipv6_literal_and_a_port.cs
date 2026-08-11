// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HostName.when_removing_the_port;

public class with_a_bracketed_ipv6_literal_and_a_port : Specification
{
    string _result;

    void Because() => _result = HostName.WithoutPort("[::ffff:10.0.0.5]:5000");

    [Fact] void should_remove_only_the_port() => _result.ShouldEqual("[::ffff:10.0.0.5]");
}
