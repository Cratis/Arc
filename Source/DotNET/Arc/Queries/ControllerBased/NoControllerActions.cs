// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Cratis.Arc.Queries.ControllerBased;

/// <summary>
/// Represents the action catalog of an application without MVC, which has no controller actions.
/// </summary>
sealed class NoControllerActions : IActionDescriptorCollectionProvider
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static readonly NoControllerActions Instance = new();

    /// <inheritdoc/>
    public ActionDescriptorCollection ActionDescriptors { get; } = new([], 0);
}
