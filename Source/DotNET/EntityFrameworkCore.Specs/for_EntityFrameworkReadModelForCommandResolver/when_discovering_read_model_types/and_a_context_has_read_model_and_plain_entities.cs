// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkReadModelForCommandResolver.when_discovering_read_model_types;

public class and_a_context_has_read_model_and_plain_entities : Specification
{
    IReadOnlyDictionary<Type, Type> _result;

    void Because() => _result = EntityFrameworkReadModelForCommandResolver.DiscoverReadModelTypes([typeof(CustomerReadModelDbContext)]);

    [Fact] void should_map_the_read_model_entity_to_its_context() => _result[typeof(CustomerReadModel)].ShouldEqual(typeof(CustomerReadModelDbContext));
    [Fact] void should_not_map_the_plain_entity() => _result.ContainsKey(typeof(PlainEntity)).ShouldBeFalse();
}
