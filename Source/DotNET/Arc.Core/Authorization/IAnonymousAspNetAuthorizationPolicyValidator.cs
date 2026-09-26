// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Validates ASP.NET Core anonymous policy opt-ins once at startup, independently of declarations.
/// </summary>
internal interface IAnonymousAspNetAuthorizationPolicyValidator
{
    /// <summary>
    /// Validates all registered opt-ins.
    /// </summary>
    /// <param name="services">The validation scope.</param>
    /// <param name="cancellationToken">Startup cancellation.</param>
    /// <returns>The validation operation.</returns>
    Task ValidateAnonymousPolicies(IServiceProvider services, CancellationToken cancellationToken);
}
