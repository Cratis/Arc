// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootEventHandlers;

#pragma warning disable SA1402, SA1649

[EventType]
public record AsyncFaultingEvent();

public class AggregateRootWithAsyncFaultingHandler : AggregateRoot
{
    public async Task OnAsyncFaulting(AsyncFaultingEvent @event)
    {
        await Task.Yield();

        // A handler that itself faults with a TargetInvocationException must surface intact — only the reflection
        // wrapper produced by MethodInfo.Invoke should be unwrapped, and an async handler faults through its awaited
        // task, not through the invoke.
        throw new TargetInvocationException(new InvalidOperationException("inner failure"));
    }
}

#pragma warning restore SA1402, SA1649
