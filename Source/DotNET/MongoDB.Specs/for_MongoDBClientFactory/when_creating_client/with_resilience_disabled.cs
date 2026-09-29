// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_creating_client;

public class with_resilience_disabled : given.a_client_factory
{
    IMongoClient _client;

    protected override void ConfigureOptions(MongoDBOptions options) => options.EnableResilience = false;

    void Because() => _client = _factory.Create("mongodb://localhost:27017");

    [Fact] void should_return_a_plain_mongo_client() => _client.ShouldBeOfExactType<MongoClient>();
    [Fact] void should_not_be_proxied() => (_client is IProxyTargetAccessor).ShouldBeFalse();
    [Fact] void should_return_databases_that_are_not_proxied() => (_client.GetDatabase("test") is IProxyTargetAccessor).ShouldBeFalse();
}
