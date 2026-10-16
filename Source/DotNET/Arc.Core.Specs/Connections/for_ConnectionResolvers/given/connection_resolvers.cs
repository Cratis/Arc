// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Connections.for_ConnectionResolvers.given;

public class connection_resolvers : Specification
{
    protected IConnectionResolverFactory _factory;
    protected ArcOptions _arcOptions;
    protected ConnectionResolvers _resolvers;

    void Establish()
    {
        _factory = Substitute.For<IConnectionResolverFactory>();
        _factory.Create(Arg.Any<ConnectionResolutionOptions>()).Returns(_ => Substitute.For<ICachingConnectionResolver>());
        _arcOptions = new();
        _resolvers = new(_factory, Options.Create(_arcOptions));
    }
}
