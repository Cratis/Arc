// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Sinks.for_MongoDBReadModelDatabases.when_getting_database;

public class for_a_read_model_in_a_named_sink : given.read_model_databases
{
    IMongoDatabase _result;

    void Establish() => _readModelSinks.GetSinkNameFor(typeof(string)).Returns("reporting");

    void Because() => _result = _databases.GetFor(typeof(string));

    [Fact] void should_give_the_database_of_the_scope_on_the_sink_server() => _result.ShouldBeSame(_sinkDatabase);
}
