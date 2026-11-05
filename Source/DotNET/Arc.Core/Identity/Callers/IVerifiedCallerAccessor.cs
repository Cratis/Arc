// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>Lazy resolution for UI, REST, and HTTP-hosted MCP. Never promotes User claims or unsigned headers.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal interface IVerifiedCallerAccessor
{
    /// <summary>Resolves at most once per request, caching success and refusal.</summary>
    /// <returns>The request's caller or refusal.</returns>
    Task<CallerResolution> GetCurrent();
}
