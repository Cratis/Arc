// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// The exception that is thrown when an explicit tenant scope is requested with a custom tenant ID accessor that cannot honor it.
/// </summary>
public class ExplicitTenantScopeRequiresArcAccessor() : Exception("ITenantScope requires TenantIdAccessor as the effective ITenantIdAccessor registration. A custom accessor cannot honor explicit tenant scopes.");
