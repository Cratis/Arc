// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Defines a source of generated identities.
/// </summary>
public interface IIdentitySource
{
    /// <summary>
    /// Generates a new identity.
    /// </summary>
    /// <returns>The generated identity.</returns>
    Guid NewGuid();
}
