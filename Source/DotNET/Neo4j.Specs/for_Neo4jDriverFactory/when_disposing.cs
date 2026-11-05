// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.for_Neo4jDriverFactory;

public class when_disposing : Specification
{
    Neo4jDriverFactory _factory;
    IDriver _driver;

    void Establish()
    {
        _driver = Substitute.For<IDriver>();
        _factory = new(Options.Create(new Neo4jOptions { Server = "bolt://configured:7687", EventStore = "Orders" }), new ConfigurationBuilder().Build(), NullLogger<Neo4jDriverFactory>.Instance, (_, _) => _driver);
        _factory.Create(new Uri("bolt://one:7687"), new BasicGraphAuthentication(null, null));
    }

    async Task Because() => await _factory.DisposeAsync();

    [Fact] void should_dispose_the_drivers() => _driver.Received(1).DisposeAsync();
}
