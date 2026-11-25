// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices;

/// <summary>Preserves a named graph projection and its declaring slice for Screenplay generation.</summary>
/// <remarks>
/// The marker's namespace supplies the declaring slice. The graph name matches Chronicle's
/// <c>[Graph("...")]</c> annotation. Members are the owned node and edge record types;
/// all handlers on a member belong to this projection. This metadata does not change runtime graph execution.
/// </remarks>
/// <param name="name">The exact projection name.</param>
/// <param name="graph">The Chronicle graph name.</param>
/// <param name="members">The node and edge record types owned by the projection.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GraphProjectionDeclarationAttribute(string name, string graph, params Type[] members) : Attribute
{
    /// <summary>Gets the projection name.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the Chronicle graph name.</summary>
    public string Graph { get; } = graph;

    /// <summary>Gets a read-only snapshot of the owned node and edge record types.</summary>
    public IReadOnlyList<Type> Members { get; } = Array.AsReadOnly(members.ToArray());
}
