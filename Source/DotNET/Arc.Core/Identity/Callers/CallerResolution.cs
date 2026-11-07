// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// A cached request resolution, including refusals. Consumers must fail closed without a caller.
/// </summary>
public sealed record CallerResolution
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallerResolution"/> class.
    /// </summary>
    /// <param name="caller">The constructed caller, or null on refusal.</param>
    /// <param name="refusal">The refusal, or null on success.</param>
    internal CallerResolution(VerifiedCaller? caller, CallerRefusal? refusal)
    {
        Caller = caller;
        Refusal = refusal;
    }

    /// <summary>
    /// Gets the constructed caller, or null on refusal.
    /// </summary>
    public VerifiedCaller? Caller { get; internal init; }

    /// <summary>
    /// Gets the refusal, or null on success.
    /// </summary>
    public CallerRefusal? Refusal { get; internal init; }
}
