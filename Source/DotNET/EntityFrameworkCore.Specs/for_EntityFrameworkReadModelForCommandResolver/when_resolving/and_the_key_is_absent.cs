// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkReadModelForCommandResolver.when_resolving;

public class and_the_key_is_absent : given.a_seeded_resolver
{
    void Establish() => SeedCustomer();

    void Because() => CatchResolveCustomerWithKey(resolvedKey: null);

    [Fact] void should_throw_unable_to_resolve_read_model_from_command_context() => _exception.ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
    [Fact] void should_be_a_validation_failure() => (_exception is IValidationFailure).ShouldBeTrue();
}
