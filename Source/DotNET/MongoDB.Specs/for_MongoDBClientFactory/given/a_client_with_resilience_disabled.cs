// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.given;

public class a_client_with_resilience_disabled : a_factory_over_a_native_client
{
    protected IMongoClient _client;
    protected IMongoCollection<Document> _collection;

    void Establish()
    {
        _options.EnableResilience = false;
        _client = _factory.Create(_settings);
        _collection = _client.GetDatabase("db").GetCollection<Document>("docs");
    }
}
