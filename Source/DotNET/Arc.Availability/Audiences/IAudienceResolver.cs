// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// Defines a system that resolves the effective audiences of a request.
/// </summary>
public interface IAudienceResolver
{
    /// <summary>
    /// Resolves the audiences that apply to the subject.
    /// </summary>
    /// <param name="subject">The subject to resolve for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The <see cref="EffectiveAudiences"/>.</returns>
    ValueTask<EffectiveAudiences> Resolve(AudienceSubject subject, CancellationToken cancellationToken = default);
}
