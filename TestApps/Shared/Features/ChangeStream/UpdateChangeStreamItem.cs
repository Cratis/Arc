// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace TestApps.Features.ChangeStream;

/// <summary>
/// Represents a command that updates an existing item in the change stream showcase collection.
/// </summary>
/// <param name="Id">The identifier of the item to update.</param>
/// <param name="Label">The new label value.</param>
/// <param name="Value">The new numeric value.</param>
[Command, AllowAnonymous]
public record UpdateChangeStreamItem(int Id, string Label, int Value)
{
    /// <summary>
    /// Handles the command by replacing the matching item in the collection.
    /// </summary>
    public void Handle() => ChangeStreamItem.Update(Id, Label, Value);
}
