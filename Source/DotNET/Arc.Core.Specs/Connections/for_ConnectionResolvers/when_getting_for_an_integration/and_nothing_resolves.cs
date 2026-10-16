// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionResolvers.when_getting_for_an_integration;

public class and_nothing_resolves : given.connection_resolvers
{
    ICachingConnectionResolver? _result;

    void Because() => _result = _resolvers.For(new ConnectionResolutionOptions());

    [Fact] void should_not_resolve() => _result.ShouldBeNull();
    [Fact] void should_not_create_a_resolver() => _factory.DidNotReceive().Create(Arg.Any<ConnectionResolutionOptions>());
}
