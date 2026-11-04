// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Identifies the framework adapter to emit with proxies.
/// </summary>
internal enum ProxyFramework
{
    /// <summary>Emit the React adapter helpers.</summary>
    React = 0,

    /// <summary>Emit framework-neutral proxies only.</summary>
    None = 1
}
