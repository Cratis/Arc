// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Transports fixed refusal reasons between internal parsing stages without a public exception type.</summary>
internal static class ForwardedIdentityV2Rejected
{
    static readonly object _reasonKey = new();

    /// <summary>Creates an implementation failure tagged with a fixed reason.</summary>
    /// <param name="reason">The fixed refusal reason.</param>
    /// <returns>A failure containing no request values.</returns>
    internal static Exception For(ForwardedIdentityV2Rejection reason)
    {
        var failure = new InvalidDataException("Forwarded identity verification refused.");
        failure.Data[_reasonKey] = reason;
        return failure;
    }

    /// <summary>Identifies a refusal from this verification pipeline.</summary>
    /// <param name="error">The failure to inspect.</param>
    /// <param name="reason">The fixed reason, when recognized.</param>
    /// <returns>Whether the failure belongs to this pipeline.</returns>
    internal static bool Is(Exception error, out ForwardedIdentityV2Rejection reason)
    {
        if (error is InvalidDataException && error.Data[_reasonKey] is ForwardedIdentityV2Rejection refusal)
        {
            reason = refusal;
            return true;
        }
        reason = default;
        return false;
    }
}
