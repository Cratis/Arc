// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.given;

/// <summary>
/// Leaves the mapping table empty after each spec.
/// </summary>
/// <remarks>
/// The table is static, so a spec that configures one and walks away changes what every later spec
/// generates - and the guarantee worth protecting here is that a build configuring nothing generates
/// exactly what it generated before.
/// </remarks>
public class no_type_mappings : Specification
{
    void Destroy() => TypeExtensions.SetTypeMappings([]);
}
