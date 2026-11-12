// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The name a part of the application is known by to clients: the full name of its command or read model type.
/// </summary>
public static class PartName
{
    /// <summary>
    /// Gets the name of a part.
    /// </summary>
    /// <param name="part">The command type, or the type declaring the query.</param>
    /// <returns>The name clients use.</returns>
    public static string Of(Type part) => part.FullName ?? part.Name;
}
