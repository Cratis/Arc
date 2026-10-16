// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Describes the evaluation of a prepared authorization plan for a bound or explicitly resource-free operation.
/// </summary>
/// <param name="Target">The protected target.</param>
/// <param name="Resource">The resource, or null after explicit resource-free admission.</param>
/// <param name="Services">The executing scope.</param>
/// <param name="LegacyVerdict">Produces the legacy verdict that applies after declared requirements are met.</param>
/// <param name="Prepared">The prepared plan.</param>
internal record PreparedAuthorizationRequest(MemberInfo Target, object? Resource, IServiceProvider Services, Func<bool> LegacyVerdict, PreparedAuthorization Prepared);
