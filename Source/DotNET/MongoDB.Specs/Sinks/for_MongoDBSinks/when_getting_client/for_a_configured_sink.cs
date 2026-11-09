// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Sinks.for_MongoDBSinks.when_getting_client;

public class for_a_configured_sink : given.mongodb_sinks
{
    IMongoClient _first;
    IMongoClient _second;

    void Establish() => _options.Sinks["Reporting"] = new() { Server = "mongodb://reporting:27017" };

    void Because()
    {
        _first = _sinks.GetClientFor("reporting");
        _second = _sinks.GetClientFor("REPORTING");
    }

    [Fact] void should_connect_to_the_sink_server() => _createdFor.Single().Server.Host.ShouldEqual("reporting");
    [Fact] void should_match_the_name_without_regard_to_case() => _second.ShouldBeSame(_first);
}
