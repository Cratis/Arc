// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBReadModelForCommandResolver.when_resolving;

public class and_the_instance_is_absent : given.a_resolver
{
    IMongoCollection<Customer> _collection;
    object? _result;

    void Establish() => _collection = CollectionHolding<Customer>();

    async Task Because() => _result = await _resolver.Resolve(typeof(Customer), CommandContextWith(Guid.NewGuid().ToString(), _collection));

    [Fact] void should_resolve_to_null() => _result.ShouldBeNull();
}
