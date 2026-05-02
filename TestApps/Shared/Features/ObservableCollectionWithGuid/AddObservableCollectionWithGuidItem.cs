// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace TestApps.Features.ObservableCollectionWithGuid;

/// <summary>
/// Adds a new item to the Guid-based observable collection test feature.
/// </summary>
/// <param name="Id">The new item identifier.</param>
/// <param name="Label">The new item label.</param>
[Command, AllowAnonymous]
public record AddObservableCollectionWithGuidItem(Guid Id, string Label)
{
    /// <summary>
    /// Handles the command by appending a new item to the collection.
    /// </summary>
    public void Handle() => ObservableCollectionWithGuidItem.Add(Id, Label);
}