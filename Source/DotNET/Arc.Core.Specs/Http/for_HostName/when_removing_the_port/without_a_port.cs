// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HostName.when_removing_the_port;

public class without_a_port : Specification
{
    string _result;

    void Because() => _result = HostName.WithoutPort("acme.myapp.com");

    [Fact] void should_keep_the_host_unchanged() => _result.ShouldEqual("acme.myapp.com");
}
