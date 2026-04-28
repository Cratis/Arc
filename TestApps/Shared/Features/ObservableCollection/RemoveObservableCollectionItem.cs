// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace TestApps.Features.ObservableCollection;

/// <summary>
/// Removes an item from the observable collection test feature.
/// </summary>
/// <param name="Id">The identifier of the item to remove.</param>
[Command, AllowAnonymous]
public record RemoveObservableCollectionItem(int Id)
{
    /// <summary>
    /// Handles the command by removing the matching item from the collection.
    /// </summary>
    public void Handle() => ObservableCollectionItem.Remove(Id);
}