// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_HttpConnectionResolver;

public class when_creating_without_an_endpoint : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _ = new HttpConnectionResolver(new(), new HttpClient()));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<HttpConnectionResolverEndpointNotConfigured>();
}
