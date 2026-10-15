// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_protecting_reads.with_live_off_siblings.given;

/// <summary>
/// An Off command starts two concurrent nested Off commands, lets one of them end while the other is still running,
/// and then starts a nested command declaring read protection On.
/// </summary>
public abstract class two_live_off_siblings : for_TransactionalCommandScope.given.a_read_protection_pipeline
{
    protected readonly TaskCompletionSource[] _started = [new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously)];
    protected readonly TaskCompletionSource[] _release = [new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously)];
    protected CommandResult _onSibling;
    protected CommandResult[] _offSiblings = [];

    /// <summary>
    /// Gets the index of the Off sibling that ends before the On command starts.
    /// </summary>
    protected abstract int EndsFirst { get; }

    void Establish() => _handle = async context =>
    {
        switch (context.Command)
        {
            case OffChild child:
                _started[child.Index].SetResult();
                await _release[child.Index].Task;
                break;
            case OnChild:
                break;
            default:
                var first = Execute(new OffChild(0));
                await _started[0].Task;
                var second = Execute(new OffChild(1));
                await _started[1].Task;
                _release[EndsFirst].SetResult();
                await (EndsFirst == 0 ? first : second);
                _onSibling = await Execute(new OnChild());
                _release[1 - EndsFirst].SetResult();
                _offSiblings = [await first, await second];
                break;
        }
        return null;
    };

    public record OffChild(int Index);

    [ReadProtection(ReadProtectionMode.On)]
    public record OnChild;
}
