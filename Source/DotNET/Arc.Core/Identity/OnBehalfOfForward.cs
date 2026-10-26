// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents the raw values of a request that an on-behalf forward is verified against.
/// </summary>
/// <param name="Principal">The exact value of <see cref="MicrosoftIdentityPlatformHeaders.PrincipalHeader"/>.</param>
/// <param name="PrincipalId">The exact value of <see cref="MicrosoftIdentityPlatformHeaders.IdentityIdHeader"/>.</param>
/// <param name="Actor">The exact value of <see cref="OnBehalfOfHeaders.ActorHeader"/>, repeated values joined by a comma.</param>
/// <param name="Assertion">The exact value of <see cref="OnBehalfOfHeaders.AssertionHeader"/>, repeated values joined by a comma.</param>
/// <param name="Tenant">The value of the configured tenant header, or <see langword="null"/> when the request has none.</param>
/// <param name="Method">The HTTP method of the request.</param>
/// <param name="Host">The host name of the request, without the port.</param>
/// <param name="Path">The full path of the request, without the query.</param>
internal sealed record OnBehalfOfForward(
    string Principal,
    string PrincipalId,
    string? Actor,
    string? Assertion,
    string? Tenant,
    string Method,
    string Host,
    string Path);
