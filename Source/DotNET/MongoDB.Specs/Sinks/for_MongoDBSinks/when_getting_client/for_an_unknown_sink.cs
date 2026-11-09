// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.Sinks.for_MongoDBSinks.when_getting_client;

public class for_an_unknown_sink : given.mongodb_sinks
{
    Exception _error;

    void Establish() => _options.Sinks["reporting"] = new() { Server = "mongodb://reporting:27017" };

    void Because() => _error = Catch.Exception(() => _sinks.GetClientFor("warehouse"));

    [Fact] void should_fail_with_unknown_sink() => _error.ShouldBeOfExactType<UnknownMongoDBSink>();
    [Fact] void should_name_the_sink() => _error.Message.ShouldContain("'warehouse'");
    [Fact] void should_name_the_configured_sinks() => _error.Message.ShouldContain("'reporting'");
}
