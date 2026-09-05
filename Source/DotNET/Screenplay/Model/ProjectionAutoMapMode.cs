// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents how automatic property mapping applies to a projection scope.
/// </summary>
public enum ProjectionAutoMapMode
{
    /// <summary>
    /// Inherit the setting of the enclosing scope.
    /// </summary>
    Inherit = 0,

    /// <summary>
    /// Do not map properties automatically.
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// Map properties automatically.
    /// </summary>
    Enabled = 2
}
