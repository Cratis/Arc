// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Describes a resource-free authorization check of an unbound command type.
/// </summary>
/// <param name="Target">The unbound command type.</param>
/// <param name="Services">The request services.</param>
/// <param name="LegacyEvaluator">The configured type evaluator.</param>
/// <param name="Prepared">An optional already prepared authentication plan.</param>
internal record TypeAuthorizationCheck(Type Target, IServiceProvider Services, IAuthorizationEvaluator LegacyEvaluator, PreparedAuthorization? Prepared = null);
