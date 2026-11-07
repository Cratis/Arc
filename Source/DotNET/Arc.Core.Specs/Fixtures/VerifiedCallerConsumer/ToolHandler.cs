// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace VerifiedCallerConsumer;

/// <summary>
/// A non-friend HTTP-hosted tool consumer. Verification alone never authorizes the operation.
/// </summary>
/// <param name="callers">The request-local caller accessor.</param>
public sealed class ToolHandler(IVerifiedCallerAccessor callers)
{
    /// <summary>
    /// Reads the verified caller only when the product's own operation policy allows it.
    /// </summary>
    /// <param name="authorize">The product's membership and operation authorization.</param>
    /// <returns>The permitted caller, or null on refusal or policy rejection.</returns>
    public async Task<VerifiedCaller?> Handle(Func<VerifiedCaller, bool> authorize)
    {
        var resolution = await callers.GetCurrent();
        return resolution.Refusal is null && resolution.Caller is { } caller && authorize(caller) ? caller : null;
    }
}
