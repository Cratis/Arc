// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_CompositeConnectionResolver;

public class when_resolving : Specification
{
    IConnectionResolver _connection;
    IConnectionResolver _identity;
    ConnectionRequest _request;
    ResolvedConnection _result;

    void Establish()
    {
        _request = new(WellKnownConnectionStorage.SqlServer, ConnectionPurpose.Runtime, new Dictionary<string, string>());
        _connection = Substitute.For<IConnectionResolver>();
        _connection.Resolve(_request, Arg.Any<CancellationToken>()).Returns(new ResolvedConnection("Server=sql-07;Database=tenant", Identity: new("from-connection", "password")));
        _identity = Substitute.For<IConnectionResolver>();
        _identity.Resolve(_request, Arg.Any<CancellationToken>()).Returns(new ResolvedConnection(Identity: new(AccessToken: "token")));
    }

    async Task Because() => _result = await new CompositeConnectionResolver([_connection, _identity]).Resolve(_request);

    [Fact] void should_keep_the_connection_string_from_the_first() => _result.ConnectionString.ShouldEqual("Server=sql-07;Database=tenant");
    [Fact] void should_take_the_identity_from_the_last() => _result.Identity.ShouldEqual(new ConnectionIdentity(AccessToken: "token"));
}
