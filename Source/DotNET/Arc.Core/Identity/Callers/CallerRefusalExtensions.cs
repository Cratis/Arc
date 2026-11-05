// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>Maps refusals for HTTP consumers without writing responses.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal static class CallerRefusalExtensions
{
    /// <summary>Gets the transport status appropriate for the refusal.</summary>
    /// <param name="refusal">The refusal.</param>
    /// <returns>400 for multiple or URI credentials; 401 for other caller-resolution refusals. Endpoint admission is separate.</returns>
    internal static int HttpStatus(this CallerRefusal refusal) => refusal switch
    {
        CallerRefusal.CredentialInUri or CallerRefusal.MultipleCredentials or CallerRefusal.MalformedAuthorization => 400,
        _ => 401
    };
}
