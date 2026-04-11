// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoDBObserveBuilder.when_joining;

public class and_no_filter_is_provided : given.an_observe_builder
{
    IMongoDBJoinedObserveBuilder<DocumentA, DocumentB> _result;

    void Because() => _result = _builder.Join<DocumentB>();

    [Fact] void should_return_a_joined_observe_builder() => _result.ShouldBeOfExactType<MongoDBJoinedObserveBuilder<DocumentA, DocumentB>>();
}
