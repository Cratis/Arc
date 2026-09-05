// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBReadModelForCommandResolver.when_resolving;

public class and_the_instance_is_present : given.a_resolver
{
    Customer _customer;
    IMongoCollection<Customer> _collection;
    object? _result;

    void Establish()
    {
        _customer = new Customer(Guid.NewGuid(), "Alice");
        _collection = CollectionHolding(_customer);
    }

    async Task Because() => _result = await _resolver.Resolve(typeof(Customer), CommandContextWith(_customer.Id.ToString(), _collection));

    [Fact] void should_resolve_the_document() => _result.ShouldEqual(_customer);
}
