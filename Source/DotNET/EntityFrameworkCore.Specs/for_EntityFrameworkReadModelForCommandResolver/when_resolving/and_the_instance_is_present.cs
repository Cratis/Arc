// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkReadModelForCommandResolver.when_resolving;

public class and_the_instance_is_present : given.a_seeded_resolver
{
    void Establish() => SeedCustomer();

    void Because() => ResolveCustomerWithKey(_customerId.ToString());

    [Fact] void should_resolve_the_read_model() => _resolved.ShouldNotBeNull();
    [Fact] void should_resolve_the_read_model_with_the_matching_key() => ((CustomerReadModel)_resolved!).Id.ShouldEqual(_customerId);
}
