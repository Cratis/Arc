// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command declaration that appends through an event source definition.
/// </summary>
/// <typeparam name="TSource">The <see cref="IEventSource"/> definition the command appends through.</typeparam>
/// <param name="stream">The optional declared stream to append to.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class EventSourceAttribute<TSource>(string? stream = default) : Attribute, IEventSourceDeclaration
    where TSource : IEventSource
{
    /// <inheritdoc/>
    public Type EventSource => typeof(TSource);

    /// <inheritdoc/>
    public string? Stream => stream;
}
