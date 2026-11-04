// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup.when_resolving;

public class and_key_traversal_is_not_configured : given.a_caller_lookup
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => CommandReadModelLookup.Resolve(_context, typeof(Command).GetMethod("Handle")!.GetParameters()[0], ReadProtectionMode.On, null, out _));

    [Fact] void should_preserve_the_configuration_refusal() => _exception.ShouldBeOfExactType<ProtectedCommandRequiresCommandAwareReader>();
}
