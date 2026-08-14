// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HostName.when_removing_the_port;

public class with_an_empty_host : Specification
{
    string _result;

    void Because() => _result = HostName.WithoutPort(string.Empty);

    [Fact] void should_return_an_empty_host() => _result.ShouldEqual(string.Empty);
}
