// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.given;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.for_Neo4jDriverFactory;

public class when_creating : Specification
{
    Neo4jDriverFactory _factory;
    int _created;
    IDriver _sameCredentials1;
    IDriver _sameCredentials2;
    IDriver _otherCredentials;
    IDriver _otherServer;
    BasicGraphAuthentication _fromSecret;

    void Establish()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["NEO4J_PASSWORD"] = "from-secret" }).Build();
        var options = Options.Create(new Neo4jOptions { Server = "bolt://configured:7687", EventStore = "Orders", UserName = "neo4j", Password = "ignored", PasswordSecret = "NEO4J_PASSWORD" });
        _factory = new(options, configuration, NullLogger<Neo4jDriverFactory>.Instance, (_, _) =>
        {
            _created++;
            return Substitute.For<IDriver>();
        });
    }

    void Because()
    {
        var server = new Uri("bolt://one:7687");
        _sameCredentials1 = _factory.Create(server, new BasicGraphAuthentication("a", "b"));
        _sameCredentials2 = _factory.Create(server, new BasicGraphAuthentication("a", "b"));
        _otherCredentials = _factory.Create(server, new BasicGraphAuthentication("a", "other"));
        _otherServer = _factory.Create(new Uri("bolt://two:7687"), new BasicGraphAuthentication("a", "b"));
        _fromSecret = _factory.GetConfiguredAuthentication();
    }

    [Fact] void should_share_the_driver_for_the_same_server_and_identity() => _sameCredentials2.ShouldEqual(_sameCredentials1);
    [Fact] void should_not_share_the_driver_between_identities() => _otherCredentials.ShouldNotEqual(_sameCredentials1);
    [Fact] void should_not_share_the_driver_between_servers() => _otherServer.ShouldNotEqual(_sameCredentials1);
    [Fact] void should_create_a_driver_per_server_and_identity() => _created.ShouldEqual(3);
    [Fact] void should_read_the_password_from_the_secret() => _fromSecret.ShouldEqual(new BasicGraphAuthentication("neo4j", "from-secret"));
}
