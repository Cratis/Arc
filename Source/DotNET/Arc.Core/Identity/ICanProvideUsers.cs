// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Defines a development provider for users.
/// </summary>
public interface ICanProvideUsers
{
    /// <summary>
    /// Provide users for development tooling.
    /// </summary>
    /// <returns>The users.</returns>
    Task<IEnumerable<User>> Provide();
}
