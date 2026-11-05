// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.given;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.for_GraphSession;

public class when_getting_the_parameters : Specification
{
    Guid _id;
    GraphSession _session;
    IDictionary<string, object> _result;

    void Establish()
    {
        _id = Guid.NewGuid();
        _session = new(new GraphConnection(Substitute.For<IDriver>(), "orders-acme"), "acme", GraphDatabaseMode.Single, AccessMode.Read);
    }

    void Because() => _result = _session.Parameters(new { PersonId = new PersonId(_id) });

    [Fact] void should_unwrap_the_concept() => _result["PersonId"].ShouldEqual(_id.ToString());
    [Fact] void should_add_the_namespace_for_filtering() => _result[GraphParameters.Namespace].ShouldEqual("acme");
}
