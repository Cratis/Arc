// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_SharedMongoClient;

public class when_disposing : given.a_shared_client
{
    void Because() => _client.Dispose();

    [Fact] void should_not_dispose_the_native_client() => _native.DidNotReceive().Dispose();
}
