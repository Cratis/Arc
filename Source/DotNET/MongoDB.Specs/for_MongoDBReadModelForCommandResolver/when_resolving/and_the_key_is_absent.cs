// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoDBReadModelForCommandResolver.when_resolving;

public class and_the_key_is_absent : given.a_resolver
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => _resolver.Resolve(typeof(Customer), CommandContextWith<Customer>(null)));

    [Fact] void should_fail_because_the_command_carries_no_key() => _exception.ShouldBeOfExactType<UnableToResolveReadModelFromCommandContext>();
}
