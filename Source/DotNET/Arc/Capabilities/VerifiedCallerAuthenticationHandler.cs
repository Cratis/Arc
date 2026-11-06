// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Presents the caller the capability gate already verified as the request's authenticated principal.
/// It never resolves a credential itself.
/// </summary>
/// <param name="options">The scheme options.</param>
/// <param name="logger">The logger factory.</param>
/// <param name="encoder">The URL encoder.</param>
internal sealed class VerifiedCallerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.Items.TryGetValue(VerifiedCallerPrincipal.CallerItemKey, out var item) && item is VerifiedCaller caller)
        {
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(VerifiedCallerPrincipal.Create(caller), Scheme.Name)));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
