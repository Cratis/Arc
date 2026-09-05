// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkReadModelForCommandResolver.when_resolving;

public class and_the_instance_is_absent : given.a_seeded_resolver
{
    void Because() => ResolveCustomerWithKey(_customerId.ToString());

    [Fact] void should_resolve_to_null() => _resolved.ShouldBeNull();
}
