// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.given;

/// <summary>
/// An item that only exposes its identity through an explicitly implemented interface property.
/// </summary>
/// <param name="identity">The identity of the item.</param>
/// <param name="name">The name of the item.</param>
public sealed class ItemWithExplicitId(Guid identity, string name) : IHaveAnIdentity
{
    public string Name { get; } = name;

    Guid IHaveAnIdentity.Id => identity;
}
