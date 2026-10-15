// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_protecting_reads;

/// <summary>
/// A nested command that declares read protection On keeps the owner boundary even when its outer command does not
/// enforce it: it cannot run beside a sibling, and it cannot commit the outer command's transaction.
/// </summary>
public class with_nested_on_commands_in_an_off_command : given.a_read_protection_pipeline
{
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _nestedRuns;
    CommandResult _first;
    CommandResult _sibling;
    Exception? _earlyCommit;

    void Establish() => _handle = async context =>
    {
        switch (context.Command)
        {
            case NestedOn when Interlocked.Increment(ref _nestedRuns) == 1:
                _started.SetResult();
                await _release.Task;
                break;
            case NestedOn:
                break;
            case CommittingOn:
                _earlyCommit = await Catch.Exception(async () => await TransactionalCommandScope.CommitAggregate(_unit));
                break;
            default:
                var first = Execute(new NestedOn());
                await _started.Task;
                _sibling = await Execute(new NestedOn());
                _release.SetResult();
                _first = await first;
                await Execute(new CommittingOn());
                break;
        }
        return null;
    };

    async Task Because() => _result = await Execute(new OffCommand());

    [Fact] void should_refuse_the_concurrent_sibling() => _sibling.ExceptionMessages.Single().ShouldContain("Run nested commands sequentially.");
    [Fact] void should_complete_the_first_nested_command() => _first.IsSuccess.ShouldBeTrue();
    [Fact] void should_refuse_committing_the_outer_transaction() => _earlyCommit.ShouldBeOfExactType<NestedEarlyCompletionUnderReadProtection>();
    [Fact] void should_succeed_the_outer_command() => _result.IsSuccess.ShouldBeTrue();

    [ReadProtection(ReadProtectionMode.On)]
    public record NestedOn;

    [ReadProtection(ReadProtectionMode.On)]
    public record CommittingOn;
}
