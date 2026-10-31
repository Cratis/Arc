// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication.given;

public class a_signed_handler : Specification
{
    protected ArcOptions _options;
    protected DefaultHttpContext _context;
    protected Dictionary<string, string> _headers;
    protected TimeProvider _clock;
    readonly ForwardedIdentityReplayCache _replays = new();

    void Establish() => ResetRequest();

    protected void ResetRequest()
    {
        _options = ForwardedIdentityForSpecs.Options();
        _headers = ForwardedIdentityForSpecs.Headers();
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: 1700000000);
        _clock = Substitute.For<TimeProvider>();
        _clock.GetUtcNow().Returns(DateTimeOffset.FromUnixTimeSeconds(1700000000));
        _context = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(Options.Create(_options)).BuildServiceProvider() };
        _context.Request.Method = "POST";
        _context.Request.PathBase = "/base";
        _context.Request.Path = "/a%2Fb";
        _context.Features.Get<IHttpRequestFeature>()!.RawTarget = "/base/a%2fb?x=1";
        ApplyHeaders();
    }

    protected void ApplyHeaders()
    {
        _context.Request.Headers.Clear();
        foreach (var pair in _headers)
        {
            _context.Request.Headers[pair.Key] = pair.Value;
        }
    }

    protected async Task<AuthenticateResult> Authenticate()
    {
        var schemeOptions = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        schemeOptions.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions { TimeProvider = _clock });
        var arcOptions = Substitute.For<IOptionsMonitor<ArcOptions>>();
        arcOptions.CurrentValue.Returns(_options);
        var handler = new IsolatedHandler(schemeOptions, arcOptions, _replays);
        await handler.InitializeAsync(new AuthenticationScheme(MicrosoftIDentityPlatformAuthHandler.SchemeName, null, typeof(MicrosoftIDentityPlatformAuthHandler)), _context);
        return await handler.AuthenticateAsync();
    }

    sealed class IsolatedHandler(IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions, IOptionsMonitor<ArcOptions> arcOptions, ForwardedIdentityReplayCache replays)
        : MicrosoftIDentityPlatformAuthHandler(schemeOptions, arcOptions, NullLoggerFactory.Instance, UrlEncoder.Default)
    {
        internal override ForwardedIdentityReplayCache ForwardedIdentityReplays => replays;
    }
}
