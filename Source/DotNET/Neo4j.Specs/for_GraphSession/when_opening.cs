// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.for_GraphSession;

public class when_opening : Specification
{
    IDriver _driver;
    GraphSession _session;
    IAsyncSession _inner;

    void Establish()
    {
        _inner = Substitute.For<IAsyncSession>();
        _driver = Substitute.For<IDriver>();
        _driver.AsyncSession(Arg.Any<Action<SessionConfigBuilder>>()).Returns(_inner);
        _session = new(new GraphConnection(_driver, "orders-acme"), "acme", GraphDatabaseMode.PerNamespace, AccessMode.Read);
    }

    async Task Because()
    {
        _ = _session.Session;
        await _session.DisposeAsync();
    }

    [Fact] void should_open_one_driver_session() => _driver.Received(1).AsyncSession(Arg.Any<Action<SessionConfigBuilder>>());
    [Fact] void should_expose_the_driver_session() => _session.Session.ShouldEqual(_inner);
    [Fact] void should_dispose_the_driver_session() => _inner.Received(1).DisposeAsync();
}
