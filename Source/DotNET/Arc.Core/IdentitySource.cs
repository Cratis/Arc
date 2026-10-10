// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Generates identities using <see cref="Guid.NewGuid"/>.
/// </summary>
public class IdentitySource : IIdentitySource
{
    /// <inheritdoc/>
    public Guid NewGuid() => Guid.NewGuid();
}
