// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.Sinks.for_MongoDBSinks.when_getting_client;

public class for_a_sink_with_a_server_file : given.mongodb_sinks
{
    string _path;

    void Establish()
    {
        _path = Path.GetTempFileName();
        File.WriteAllText(_path, "mongodb://from-file:27017\n");
        _options.Sinks["documents"] = new() { ServerFile = _path };
    }

    void Because() => _sinks.GetClientFor("documents");

    void Destroy() => File.Delete(_path);

    [Fact] void should_connect_to_the_server_in_the_file() => _createdFor.Single().Server.Host.ShouldEqual("from-file");
}
