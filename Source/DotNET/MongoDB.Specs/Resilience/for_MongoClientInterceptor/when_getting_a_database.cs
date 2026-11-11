// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Resilience.for_MongoClientInterceptor;

public class when_getting_a_database : Specification
{
    MongoClientInterceptor _interceptor;
    IInvocation _invocation;
    IMongoDatabase _database;
    object _returned;
    IMongoCollection<string> _collection;

    void Establish()
    {
        var client = Substitute.For<IMongoClient>();
        client.Settings.Returns(new MongoClientSettings());
        _interceptor = new(new ProxyGenerator(), Polly.ResiliencePipeline.Empty, client);
        _database = Substitute.For<IMongoDatabase>();
        _database.GetCollection<string>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(Substitute.For<IMongoCollection<string>>());
        _invocation = Substitute.For<IInvocation>();
        _invocation.ReturnValue.Returns(_database);
        _invocation.When(_ => _.ReturnValue = Arg.Any<object>()).Do(call => _returned = call.Arg<object>());
    }

    void Because()
    {
        _interceptor.Intercept(_invocation);
        _collection = ((IMongoDatabase)_returned).GetCollection<string>("documents");
    }

    [Fact] void should_return_a_resilient_database() => _returned.ShouldBeOfExactType<ResilientMongoDatabase>();
    [Fact] void should_hand_out_resilient_collections() => _collection.ShouldBeOfExactType<ResilientMongoCollection<string>>();
}
