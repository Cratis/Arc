// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class disposing_the_client : given.a_client_with_resilience_enabled
{
    IMongoClient _cached;

    void Because()
    {
        _client.Dispose();
        _cached = _factory.Create(_settings);
    }

    [Fact] void should_not_dispose_the_native_client() => _native.DidNotReceive().Dispose();
    [Fact] void should_return_the_cached_client() => _cached.ShouldBeSame(_client);
}
