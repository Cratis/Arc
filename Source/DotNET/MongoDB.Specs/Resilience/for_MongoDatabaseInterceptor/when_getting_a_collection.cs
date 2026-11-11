// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Resilience.for_MongoDatabaseInterceptor;

public class when_getting_a_collection : Specification
{
    MongoDatabaseInterceptor _interceptor;
    IInvocation _invocation;
    IMongoCollection<string> _collection;
    object _returned;

    void Establish()
    {
        var client = Substitute.For<IMongoClient>();
        client.Settings.Returns(new MongoClientSettings());
        _interceptor = new(new ProxyGenerator(), Polly.ResiliencePipeline.Empty, client);
        _collection = Substitute.For<IMongoCollection<string>>();
        _invocation = Substitute.For<IInvocation>();
        _invocation.Method.Returns(typeof(IMongoDatabase).GetMethod(nameof(IMongoDatabase.GetCollection))!.MakeGenericMethod(typeof(string)));
        _invocation.ReturnValue.Returns(_collection);
        _invocation.When(_ => _.ReturnValue = Arg.Any<object>()).Do(call => _returned = call.Arg<object>());
    }

    void Because() => _interceptor.Intercept(_invocation);

    [Fact] void should_return_a_resilient_collection() => _returned.ShouldBeOfExactType<ResilientMongoCollection<string>>();
    [Fact] void should_not_generate_a_proxy_type() => _returned.GetType().Assembly.ShouldEqual(typeof(ResilientMongoCollection<>).Assembly);
}
