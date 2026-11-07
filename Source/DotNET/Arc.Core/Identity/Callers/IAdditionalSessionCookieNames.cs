// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Internal seam through which a host supplies session cookie names that <see cref="CredentialPresentationGate"/> counts
/// in addition to its fixed set. Arc.Core has no web framework dependency, so the ASP.NET host implements this from its
/// registered cookie authentication schemes. When no implementation is registered only the fixed set applies.
/// </summary>
internal interface IAdditionalSessionCookieNames
{
    /// <summary>Gets the additional cookie names. Names are matched case-insensitively, including numbered chunk suffixes.</summary>
    IReadOnlyCollection<string> Names { get; }
}
