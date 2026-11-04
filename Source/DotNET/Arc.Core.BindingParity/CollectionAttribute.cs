// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Accepts the xUnit collection of the linked scenarios without a dependency on xUnit; the harness runs them one at a time.
/// </summary>
/// <param name="name">The name of the collection.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CollectionAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public string Name { get; } = name;
}
