// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Integration;

/// <summary>
/// Entity used for integration testing WebSocket observable queries.
/// </summary>
public class IntegrationTestEntity
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the entity is active.
    /// </summary>
    public bool IsActive { get; set; }
}
