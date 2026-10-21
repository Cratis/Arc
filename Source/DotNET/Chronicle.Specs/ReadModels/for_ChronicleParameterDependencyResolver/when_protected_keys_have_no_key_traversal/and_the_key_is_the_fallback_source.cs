// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.when_protected_keys_have_no_key_traversal;

public class and_the_key_is_the_fallback_source : given.a_parameter_lookup
{
    Exception _failure;

    void Establish() => Configure(new Source("subject"));

    void Because() => _failure = Catch.Exception(() => CommandReadModelLookup.Resolve(_context, typeof(Source).GetMethod(nameof(Source.Provide))!.GetParameters()[0], ReadProtectionMode.On, null, out _));

    [Fact] void should_fail_closed_without_a_command_aware_reader() => _failure.ShouldBeOfExactType<ProtectedCommandRequiresCommandAwareReader>();
    [Fact] void should_not_read_a_model() => _readModels.ReceivedCalls().ShouldBeEmpty();

    public record Source(string Name)
    {
        public Balance Provide(Balance balance) => balance;
    }
}
