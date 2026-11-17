// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Chronicle.Tenancy;

/// <summary>
/// Exception that gets thrown when a tenant ID cannot be used as a Chronicle namespace.
/// </summary>
/// <param name="tenantId">The tenant ID that was refused.</param>
public class TenantIdIsNotAValidNamespace(TenantId tenantId) : Exception(
    $"The tenant ID '{tenantId.Value}' cannot be used as a Chronicle namespace because it contains a comma. " +
    "A comma means the tenant was sent more than once, for example a repeated header folded into one value, and a namespace " +
    "is created the first time a name is used. Make the ingress send the tenant exactly once.");
