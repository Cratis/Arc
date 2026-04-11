// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace TestApps.Features.ChangeStream;

/// <summary>
/// Represents a command that adds a new item to the change stream showcase collection.
/// </summary>
/// <param name="Id">The identifier for the new item.</param>
/// <param name="Label">The label for the new item.</param>
/// <param name="Value">The numeric value for the new item.</param>
[Command, AllowAnonymous]
public record AddChangeStreamItem(int Id, string Label, int Value)
{
    /// <summary>
    /// Handles the command by appending the new item to the collection.
    /// </summary>
    public void Handle() => ChangeStreamItem.Add(Id, Label, Value);
}
