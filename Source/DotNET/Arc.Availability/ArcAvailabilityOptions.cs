// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability;
using Cratis.Choreography.Availability.Context;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents the availability gates of an Arc application that are not declared with attributes.
/// </summary>
/// <remarks>
/// Gates are combined with and down the path: the gates of enclosing namespaces (outermost first), then the gate
/// registered for the part itself, then the <see cref="AvailableForAttribute"/> and <see cref="AvailableWithAttribute"/>
/// declared on it.
/// </remarks>
public class ArcAvailabilityOptions
{
    readonly List<(string Namespace, AvailabilityExpression Gate)> _namespaces = [];
    readonly List<(Type Part, AvailabilityExpression Gate)> _parts = [];

    /// <summary>
    /// Gets or sets the context scope a part is decided in. Defaults to <see cref="ContextScope.Application"/>.
    /// </summary>
    /// <remarks>
    /// Context values declared for a narrower scope are visible only to the parts decided in it.
    /// </remarks>
    public Func<Type, ContextScope> ScopeFor { get; set; } = _ => ContextScope.Application;

    /// <summary>
    /// Gets the gates of namespaces.
    /// </summary>
    internal IReadOnlyList<(string Namespace, AvailabilityExpression Gate)> Namespaces => _namespaces;

    /// <summary>
    /// Gets the gates of individual parts.
    /// </summary>
    internal IReadOnlyList<(Type Part, AvailabilityExpression Gate)> Parts => _parts;

    /// <summary>
    /// Gates a command, or the queries of a read model.
    /// </summary>
    /// <typeparam name="TPart">The command type, or the read model declaring the queries.</typeparam>
    /// <param name="gate">The <see cref="AvailabilityExpression"/>.</param>
    /// <returns>The options for continuation.</returns>
    public ArcAvailabilityOptions Gate<TPart>(AvailabilityExpression gate) => Gate(typeof(TPart), gate);

    /// <summary>
    /// Gates a command, or the queries of a read model.
    /// </summary>
    /// <param name="part">The command type, or the read model declaring the queries.</param>
    /// <param name="gate">The <see cref="AvailabilityExpression"/>.</param>
    /// <returns>The options for continuation.</returns>
    public ArcAvailabilityOptions Gate(Type part, AvailabilityExpression gate)
    {
        _parts.Add((part, gate));
        return this;
    }

    /// <summary>
    /// Gates every command and query in a namespace and the namespaces below it, such as a module or a feature.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <param name="gate">The <see cref="AvailabilityExpression"/>.</param>
    /// <returns>The options for continuation.</returns>
    public ArcAvailabilityOptions GateNamespace(string @namespace, AvailabilityExpression gate)
    {
        _namespaces.Add((@namespace, gate));
        return this;
    }
}
