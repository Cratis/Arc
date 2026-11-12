// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>The immutable PAT installation ceiling, shared without exposing PAT internals.</summary>
/// <param name="settings">The validated installation snapshot.</param>
internal sealed class PatCredentialScopeAllowlist(PatSettings settings) : ICredentialScopeAllowlist
{
    /// <inheritdoc/>
    public CallerCredentialKind Kind => CallerCredentialKind.PersonalAccessToken;

    /// <inheritdoc/>
    public IReadOnlySet<string> Scopes => settings.AllowedScopes;
}
