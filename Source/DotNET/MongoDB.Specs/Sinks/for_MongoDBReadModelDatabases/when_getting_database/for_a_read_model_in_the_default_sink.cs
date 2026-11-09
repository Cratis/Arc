// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Sinks.for_MongoDBReadModelDatabases.when_getting_database;

public class for_a_read_model_in_the_default_sink : given.read_model_databases
{
    IMongoDatabase _result;

    void Establish() => _readModelSinks.GetSinkNameFor(typeof(string)).Returns((string?)null);

    void Because() => _result = _databases.GetFor(typeof(string));

    [Fact] void should_give_the_default_database() => _result.ShouldBeSame(_defaultDatabase);
    [Fact] void should_not_use_any_sink() => _sinks.DidNotReceive().GetClientFor(Arg.Any<string>());
}
