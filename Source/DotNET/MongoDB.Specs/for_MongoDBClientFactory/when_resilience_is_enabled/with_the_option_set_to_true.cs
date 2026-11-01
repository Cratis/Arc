// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class with_the_option_set_to_true : given.a_factory_over_a_native_client
{
    IMongoClient _client;

    void Because()
    {
        _options.EnableResilience = true;
        _client = _factory.Create(_settings);
    }

    [Fact] void should_keep_the_native_client_behind_the_proxy() => ((IProxyTargetAccessor)_client).DynProxyGetTarget().ShouldBeSame(_native);
}
