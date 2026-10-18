// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Configures composite member coverage for protected read-model keys.
/// </summary>
public sealed class ProtectedReadModelKeyOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether every exact composite key type requires generated or registered accessors.
    /// </summary>
    /// <remarks>
    /// Reflection compatibility is available only when this is false and dynamic code is supported.
    /// Restricted runtimes always require accessors, regardless of this setting.
    /// </remarks>
    public bool Strict { get; set; }
}
