// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Validates host-specific protection before catalog routes are mapped.
/// </summary>
internal interface IIntrospectionExposureGuard
{
    /// <summary>
    /// Refuses protected catalogs that the host cannot enforce safely.
    /// </summary>
    /// <param name="options">The catalog exposure settings.</param>
    void Validate(IntrospectionOptions options);
}
