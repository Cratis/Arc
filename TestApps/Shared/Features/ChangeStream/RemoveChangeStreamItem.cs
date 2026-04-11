// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace TestApps.Features.ChangeStream;

/// <summary>
/// Represents a command that removes an item from the change stream showcase collection.
/// </summary>
/// <param name="Id">The identifier of the item to remove.</param>
[Command, AllowAnonymous]
public record RemoveChangeStreamItem(int Id)
{
    /// <summary>
    /// Handles the command by filtering out the matching item from the collection.
    /// </summary>
    public void Handle() => ChangeStreamItem.Remove(Id);
}
