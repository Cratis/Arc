// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_creating_client;

public class with_default_options : given.a_client_factory
{
    IMongoClient _client;

    void Because() => _client = _factory.Create("mongodb://localhost:27017");

    [Fact] void should_wrap_the_client_in_the_resilience_layer() => (_client is IProxyTargetAccessor).ShouldBeTrue();
    [Fact] void should_wrap_a_mongo_client() => ((IProxyTargetAccessor)_client).DynProxyGetTarget().ShouldBeOfExactType<MongoClient>();
}
