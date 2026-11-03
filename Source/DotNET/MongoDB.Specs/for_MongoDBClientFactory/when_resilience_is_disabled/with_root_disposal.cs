// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_disabled;

public class with_root_disposal : given.a_client_with_resilience_disabled
{
    void Because() => _client.Dispose();

    [Fact] void should_not_dispose_the_shared_native_client() => _native.DidNotReceive().Dispose();
    [Fact] void should_still_return_the_native_database() => _client.GetDatabase("db").ShouldBeSame(_nativeDatabase);
}
