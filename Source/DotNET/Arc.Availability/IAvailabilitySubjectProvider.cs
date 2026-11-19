// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability;

/// <summary>
/// Defines the provider of who a request is, for resolving the audiences that apply to it.
/// </summary>
/// <remarks>
/// The default provider uses the tenant of the request and the subject of the current principal. Register another to
/// map identities differently.
/// </remarks>
public interface IAvailabilitySubjectProvider
{
    /// <summary>
    /// Gets the <see cref="AudienceSubject"/> of the current request.
    /// </summary>
    /// <returns>The subject.</returns>
    AudienceSubject GetSubject();
}
