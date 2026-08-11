// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HostName.when_removing_the_port;

public class with_a_port : Specification
{
    string _result;

    void Because() => _result = HostName.WithoutPort("acme.myapp.com:5000");

    [Fact] void should_remove_the_port() => _result.ShouldEqual("acme.myapp.com");
}
