// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace TestApps.Features.ObservableCollectionWithGuid;

/// <summary>
/// Removes an item from the Guid-based observable collection test feature.
/// </summary>
/// <param name="Id">The identifier of the item to remove.</param>
[Command, AllowAnonymous]
public record RemoveObservableCollectionWithGuidItem(Guid Id)
{
    /// <summary>
    /// Handles the command by removing the matching item from the collection.
    /// </summary>
    public void Handle() => ObservableCollectionWithGuidItem.Remove(Id);
}