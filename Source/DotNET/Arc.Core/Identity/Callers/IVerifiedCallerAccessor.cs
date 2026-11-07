// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Lazy request-local resolution for UI, REST, and HTTP-hosted tools.
/// Never promotes User claims or unsigned headers. Verification is not authorization.
/// </summary>
public interface IVerifiedCallerAccessor
{
    /// <summary>
    /// Resolves at most once per unchanged request, caching success and refusal. Refuses outside a request.
    /// </summary>
    /// <returns>The request's caller or refusal.</returns>
    Task<CallerResolution> GetCurrent();
}
