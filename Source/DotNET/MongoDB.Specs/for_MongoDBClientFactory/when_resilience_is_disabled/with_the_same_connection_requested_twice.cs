// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_disabled;

public class with_the_same_connection_requested_twice : given.a_client_with_resilience_disabled
{
    IMongoClient _result;

    void Because() => _result = _factory.Create(_settings);

    [Fact] void should_return_the_cached_client() => _result.ShouldBeSame(_client);
}
