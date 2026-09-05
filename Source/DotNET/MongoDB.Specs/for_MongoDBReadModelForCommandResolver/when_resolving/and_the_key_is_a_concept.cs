// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBReadModelForCommandResolver.when_resolving;

public class and_the_key_is_a_concept : given.a_resolver
{
    Account _account;
    IMongoCollection<Account> _collection;
    object? _result;

    void Establish()
    {
        _account = new Account(new CustomerId(Guid.NewGuid()), 42m);
        _collection = CollectionHolding(_account);
    }

    async Task Because() => _result = await _resolver.Resolve(typeof(Account), CommandContextWith(_account.Id.Value.ToString(), _collection));

    [Fact] void should_resolve_the_document() => _result.ShouldEqual(_account);
}
