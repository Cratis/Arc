// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents an <see cref="AuthenticationHandler{TOptions}"/> for handling authentication in the context of Microsoft Identity Platform.
/// </summary>
/// <remarks>
/// The forwarded identity headers are not signed. The handler ignores them and returns no result, so the request stays
/// anonymous, unless <see cref="ArcOptions.TrustForwardedIdentityHeaders"/> is enabled because the host runs behind a
/// trusted ingress. A request carrying <see cref="OnBehalfOfHeaders.ActorHeader"/> or
/// <see cref="OnBehalfOfHeaders.AssertionHeader"/> fails authentication unless <see cref="OnBehalfOfOptions.Enabled"/> is
/// set and its signed assertion verifies; when accepted, the actor becomes the <see cref="ClaimsIdentity.Actor"/> of the
/// principal.
/// </remarks>
public class MicrosoftIDentityPlatformAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Gets the scheme name.
    /// </summary>
    public const string SchemeName = "MicrosoftIdentityPlatform";

    readonly ILogger<MicrosoftIDentityPlatformAuthHandler> _logger;
    readonly IOptionsMonitor<ArcOptions>? _arcOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftIDentityPlatformAuthHandler"/> class.
    /// </summary>
    /// <param name="options">The <see cref="IOptionsMonitor{TOptions}"/>.</param>
    /// <param name="arcOptions">The <see cref="ArcOptions"/> that decide whether forwarded identity headers are trusted.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    [ActivatorUtilitiesConstructor]
    public MicrosoftIDentityPlatformAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        IOptionsMonitor<ArcOptions> arcOptions,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder)
        : base(options, loggerFactory, encoder)
    {
        _arcOptions = arcOptions;
        _logger = loggerFactory.CreateLogger<MicrosoftIDentityPlatformAuthHandler>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftIDentityPlatformAuthHandler"/> class,
    /// resolving the host's trust setting from request services when authentication runs.
    /// </summary>
    /// <param name="options">The <see cref="IOptionsMonitor{TOptions}"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    [Obsolete("Use the constructor accepting IOptionsMonitor<ArcOptions>. Forwarded identity headers are untrusted unless the host opts in.")]
    public MicrosoftIDentityPlatformAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder)
        : base(options, loggerFactory, encoder)
    {
        _logger = loggerFactory.CreateLogger<MicrosoftIDentityPlatformAuthHandler>();
    }

    /// <summary>
    /// Gets the process-local replay cache, overridable internally for isolated verification clocks in specs.
    /// </summary>
    internal virtual ForwardedIdentityReplayCache ForwardedIdentityReplays => ForwardedIdentityReplayCache.Shared;

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var arcOptions = _arcOptions ?? Context.RequestServices.GetService<IOptionsMonitor<ArcOptions>>();
        if (arcOptions?.CurrentValue.ForwardedIdentity?.Enabled == true)
        {
            return AuthenticateSigned(arcOptions.CurrentValue);
        }
        var onBehalfOf = ForwardedActor.IsPresent(Request.Headers.ContainsKey);
        if (onBehalfOf && ForwardedActor.Admit(arcOptions?.CurrentValue) is { } rejection)
        {
            return Reject(rejection);
        }

        if (arcOptions?.CurrentValue.TrustForwardedIdentityHeaders != true)
        {
            if (UntrustedForwardedIdentityHeaders.ArePresent(Request.Headers.ContainsKey))
            {
                UntrustedForwardedIdentityHeaders.Report(_logger);
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Request.IsValidIdentityRequest())
        {
            return onBehalfOf ? Reject(OnBehalfOfFailures.MissingRepresentedUser) : Task.FromResult(AuthenticateResult.Fail("Not authenticated - headers missing"));
        }

        ClientPrincipal? clientPrincipal = null;
        try
        {
            clientPrincipal = Request.GetClientPrincipal();
        }
        catch (Exception ex)
        {
            _logger.FailedResolvingClientPrincipal(ex);
        }

        if (clientPrincipal == null)
        {
            const string invalidPrincipal = "Not authenticated - invalid representation of ClientPrincipal";
            return onBehalfOf ? Reject(invalidPrincipal) : Task.FromResult(AuthenticateResult.Fail(invalidPrincipal));
        }

        var claims = ForwardedIdentityClaims.From(clientPrincipal, Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader].ToString());
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        if (onBehalfOf)
        {
            var failure = ForwardedActor.Attach(
                ForwardFrom(arcOptions.CurrentValue),
                clientPrincipal,
                identity,
                Scheme.Name,
                arcOptions.CurrentValue,
                TimeProvider.System,
                OnBehalfOfReplayCache.Shared);
            if (failure is not null)
            {
                return Reject(failure);
            }
        }

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    Task<AuthenticateResult> AuthenticateSigned(ArcOptions options)
    {
        var signed = ForwardedIdentityAuthentication.Authenticate(
            new AspNetCore.Http.AspNetCoreHttpRequestContext(Context),
            options,
            Scheme.Name,
            Request.PathBase.Add(Request.Path).ToUriComponent(),
            TimeProvider,
            ForwardedIdentityReplays);
        if (signed.Failure is { } failure)
        {
            ForwardedIdentityLogMessages.Rejected(_logger, failure);
            return Task.FromResult(AuthenticateResult.Fail(failure));
        }
        return Task.FromResult(signed.Principal is { } signedPrincipal
            ? AuthenticateResult.Success(new AuthenticationTicket(signedPrincipal, Scheme.Name))
            : AuthenticateResult.NoResult());
    }

    OnBehalfOfForward ForwardFrom(ArcOptions options) => new(
        Request.Headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader].ToString(),
        Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader].ToString(),
        HeaderOrNull(OnBehalfOfHeaders.ActorHeader),
        HeaderOrNull(OnBehalfOfHeaders.AssertionHeader),
        options.Tenancy?.HttpHeader is { Length: > 0 } tenantHeader ? HeaderOrNull(tenantHeader) : null,
        Request.Method,
        Request.PathBase.Add(Request.Path).ToUriComponent());

    string? HeaderOrNull(string name) => Request.Headers.TryGetValue(name, out var value) ? value.ToString() : null;

    Task<AuthenticateResult> Reject(string reason)
    {
        RejectedOnBehalfOfForwards.Report(_logger, reason);
        return Task.FromResult(AuthenticateResult.Fail(reason));
    }
}
