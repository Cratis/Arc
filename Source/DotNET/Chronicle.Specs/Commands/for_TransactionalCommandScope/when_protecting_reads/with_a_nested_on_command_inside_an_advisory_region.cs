// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_protecting_reads;

/// <summary>
/// An advisory region opened by an outer command body, or by a nested command that does not enable read protection,
/// does not cross into a nested command that does: that command's reads stay protected.
/// </summary>
public class with_a_nested_on_command_inside_an_advisory_region : given.a_read_protection_pipeline
{
    Exception? _insideNestedOn;
    Exception? _insideOnUnderOff;
    Exception? _outerAfterNested;
    CommandResult _nested;
    CommandResult _grandchild;

    void Establish() => _handle = async context =>
    {
        switch (context.Command)
        {
            case NestedOn:
                _insideNestedOn = ProbeAdvisory();
                break;
            case GrandchildOn:
                _insideOnUnderOff = ProbeAdvisory();
                break;
            case OffCommand:
                _grandchild = await Execute(new GrandchildOn());
                break;
            default:
                using (_unit.AllowReads(ReadAllowance.Advisory, "outer body"))
                {
                    _nested = await Execute(new NestedOn());
                    _outerAfterNested = ProbeAdvisory();
                }
                (await Execute(new OffCommand())).IsSuccess.ShouldBeTrue();
                break;
        }
        return null;
    };

    async Task Because() => _result = await Execute();

    [Fact] void should_not_carry_the_outer_body_region_into_a_nested_on_command() => _insideNestedOn.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
    [Fact] void should_not_carry_a_nested_off_command_region_into_its_on_command() => _insideOnUnderOff.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
    [Fact] void should_restore_the_outer_body_region_after_the_nested_command() => _outerAfterNested.ShouldBeNull();
    [Fact] void should_succeed_the_nested_command() => _nested.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_the_grandchild_command() => _grandchild.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    static Exception? ProbeAdvisory()
    {
        // Binding a region to another unit is allowed only inside an advisory region.
        var (_, _, other) = DecisionFixtures.Transaction();
        return Catch.Exception(() =>
        {
            using var region = other.AllowReads(ReadAllowance.Contributed, "probe");
        });
    }

    [ReadProtection(ReadProtectionMode.On)]
    public record NestedOn;

    [ReadProtection(ReadProtectionMode.On)]
    public record GrandchildOn;
}
