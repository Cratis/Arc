// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ResolvedConnection.when_merging;

public class and_the_other_sets_every_part : Specification
{
    ResolvedConnection _first;
    ResolvedConnection _second;
    ResolvedConnection _result;

    void Establish()
    {
        _first = new("first-connection", "first-database", "first-schema", new("first-user", "first-password"));
        _second = new("second-connection", "second-database", "second-schema", new(AccessToken: "token"));
    }

    void Because() => _result = _first.Merge(_second);

    [Fact] void should_take_the_connection_string_of_the_other() => _result.ConnectionString.ShouldEqual("second-connection");
    [Fact] void should_take_the_database_of_the_other() => _result.Database.ShouldEqual("second-database");
    [Fact] void should_take_the_schema_of_the_other() => _result.Schema.ShouldEqual("second-schema");
    [Fact] void should_replace_the_identity_as_a_whole() => _result.Identity.ShouldEqual(new ConnectionIdentity(AccessToken: "token"));
}
