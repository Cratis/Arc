// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_SharedMongoClient;

public class when_getting_a_database : given.a_shared_client
{
    readonly MongoDatabaseSettings _settings = new();
    IMongoDatabase _database;
    IMongoDatabase _result;

    void Establish()
    {
        _database = Substitute.For<IMongoDatabase>();
        _native.GetDatabase("db", _settings).Returns(_database);
    }

    void Because() => _result = _client.GetDatabase("db", _settings);

    [Fact] void should_forward_the_arguments() => _native.Received(1).GetDatabase("db", _settings);
    [Fact] void should_return_the_native_database() => _result.ShouldBeSame(_database);
}
