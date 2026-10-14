// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_measure_redirect_getter_fails : given.a_parameter_lookup
{
    ReadModelKey _key;
    Exception _error;

    void Establish() => Configure(new FailingConvention());
    void Because() => _error = Catch.Exception(() => _key = CommandReadModelLookup.Resolve(
        _context,
        typeof(FailingConvention).GetMethod(nameof(FailingConvention.Provide))!.GetParameters()[0],
        ReadProtectionMode.Measure));

    [Fact] void should_not_change_the_result() => _key.Value.ShouldEqual("subject");
    [Fact] void should_not_fail_the_command() => _error.ShouldBeNull();
    [Fact] void should_not_read_a_model() => _readModels.ReceivedCalls().ShouldBeEmpty();

    public record FailingConvention
    {
        public AccountId Account => throw new NotSupportedException();
        public Balance Provide(Balance balance) => balance;
    }
}
