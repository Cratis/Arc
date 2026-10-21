// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_protecting_reads;

public class with_a_nested_off_command : given.a_read_protection_pipeline
{
    Exception? _inside;
    Exception? _afterNested;
    bool _inherited;
    void Establish() => _handle = async context =>
    {
        if (context.Command is OffCommand)
        {
            _inherited = ReferenceEquals(CommandTransaction.Current, _unit) && _unit.IsEnforcingReadProtection;
            _inside = ProbeFrame();
        }
        else
        {
            await Execute(new OffCommand());
            _afterNested = ProbeFrame();
        }
        return null;
    };
    async Task Because() => _result = await Execute();

    [Fact] void should_reuse_the_enforcing_unit() => _inherited.ShouldBeTrue();
    [Fact] void should_not_end_enforcement_inside_off() => _inside.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
    [Fact] void should_not_end_enforcement_after_off() => _afterNested.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
