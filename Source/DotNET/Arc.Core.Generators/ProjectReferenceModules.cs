// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators;

/// <summary>
/// Represents the project references of an executable the <see cref="ProjectReferenceModulesGenerator"/> registers.
/// </summary>
internal sealed record ProjectReferenceModules
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectReferenceModules"/> class.
    /// </summary>
    /// <param name="reachable">The project references generated code can name a type in.</param>
    /// <param name="byName">The names of the project references generated code cannot name a type in.</param>
    public ProjectReferenceModules(EquatableArray<ReachableProjectReference> reachable, EquatableArray<string> byName)
    {
        Reachable = reachable;
        ByName = byName;
    }

    /// <summary>
    /// Gets the project references generated code can name a type in.
    /// </summary>
    public EquatableArray<ReachableProjectReference> Reachable { get; }

    /// <summary>
    /// Gets the names of the project references generated code cannot name a type in, loaded by name at runtime.
    /// </summary>
    public EquatableArray<string> ByName { get; }
}
