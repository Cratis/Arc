// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_convention_is_ambiguous : given.a_parameter_lookup
{
    Exception _error;
    void Establish() => Configure(new Ambiguous(new("from"), new("to")));
    void Because() => _error = Catch.Exception(() => CommandReadModelLookup.Resolve(_context, typeof(Ambiguous).GetMethod(nameof(Ambiguous.Provide))!.GetParameters()[0], ReadProtectionMode.On));
    [Fact] void should_require_an_explicit_lookup() => _error.ShouldBeOfExactType<AmbiguousReadModelLookup>();

    record Ambiguous(AccountId From, AccountId To)
    {
        public Balance Provide(Balance balance) => balance;
    }
}
