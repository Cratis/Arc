// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Headers for the Microsoft Identity Platform.
/// </summary>
public static class MicrosoftIdentityPlatformHeaders
{
    /// <summary>
    /// The principal header.
    /// </summary>
    public const string PrincipalHeader = "x-ms-client-principal";

    /// <summary>
    /// The identity id header.
    /// </summary>
    public const string IdentityIdHeader = "x-ms-client-principal-id";

    /// <summary>
    /// The identity name header.
    /// </summary>
    public const string IdentityNameHeader = "x-ms-client-principal-name";
}
