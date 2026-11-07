// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>A secret-free refusal or a one-time reveal handle, never a raw bearer value.</summary>
public sealed class PatIssueResult
{
    /// <summary>Creates an outcome without exposing raw credential material.</summary>
    /// <param name="token">The single-use handle, if committed.</param>
    /// <param name="refusalCode">The refusal code, if refused.</param>
    internal PatIssueResult(IssuedPat? token, string? refusalCode)
    {
        Token = token;
        RefusalCode = refusalCode;
    }

    /// <summary>Gets the reveal handle only after a successful commit.</summary>
    public IssuedPat? Token { get; }

    /// <summary>Gets a stable machine-readable refusal code, if refused.</summary>
    public string? RefusalCode { get; }

    /// <inheritdoc/>
    public override string ToString() => nameof(PatIssueResult);
}
