// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootEventHandlers"/>.
/// </summary>
public class AggregateRootEventHandlers : IAggregateRootEventHandlers
{
    readonly Dictionary<Type, MethodInfo> _methodsByEventType;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootEventHandlers"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    /// <param name="aggregateRootType">Type of <see cref="IAggregateRoot"/>.</param>
    public AggregateRootEventHandlers(IEventTypes eventTypes, Type aggregateRootType)
    {
        _methodsByEventType = aggregateRootType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                        .Where(_ => _.IsEventHandlerMethod(eventTypes.AllClrTypes))
                                        .SelectMany(_ => _.GetParameters()[0].ParameterType.GetEventTypes(eventTypes.AllClrTypes).Select(eventType => (eventType, method: _)))
                                        .ToDictionary(_ => _.eventType, _ => _.method);

        EventTypes = _methodsByEventType.Keys.Select(_ => _.GetEventType()).ToImmutableList();
    }

    /// <inheritdoc/>
    public bool HasHandleMethods => _methodsByEventType.Count > 0;

    /// <inheritdoc/>
    public IImmutableList<EventType> EventTypes { get; }

    /// <inheritdoc/>
    public async Task Handle(IAggregateRoot target, IEnumerable<EventAndContext> events, Action<EventAndContext>? onHandledEvent = default)
    {
        if (_methodsByEventType.Count == 0) return;

        foreach (var eventAndContext in events)
        {
            var eventType = eventAndContext.Event.GetType();
            if (_methodsByEventType.TryGetValue(eventType, out var method))
            {
                object[] arguments = method.GetParameters().Length == 2
                    ? [eventAndContext.Event, eventAndContext.Context]
                    : [eventAndContext.Event];

                try
                {
                    var returnValue = (Task?)method.Invoke(target, arguments);
                    if (returnValue is not null)
                    {
                        await returnValue;
                    }
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    // A void or synchronous Task handler that throws surfaces the real exception wrapped in a
                    // TargetInvocationException from MethodInfo.Invoke. Unwrap it — as the command handler path does
                    // — so callers see the actual exception type and stack, not the reflection wrapper.
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    throw;
                }

                onHandledEvent?.Invoke(eventAndContext);
            }
        }
    }
}
