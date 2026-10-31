// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHttpListener;

public class when_receiving_hostile_forwarding : Specification
{
    AuthenticationResult _valid;
    AuthenticationResult _forged;
    AuthenticationResult _expired;
    AuthenticationResult _wrongKey;
    AuthenticationResult _replayed;
    AuthenticationResult _duplicate;
    AuthenticationResult _disabled;
    AuthenticationResult _unsigned;
    AuthenticationResult _reentered;

    async Task Because()
    {
        var headers = ForwardedIdentityForSpecs.Headers();
        headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(headers);
        _valid = await Authenticate(headers, reenter: true);
        _replayed = await Authenticate(headers);
        headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = "forged";
        _forged = await Authenticate(headers);
        headers = ForwardedIdentityForSpecs.Headers();
        headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(headers, timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 70);
        _expired = await Authenticate(headers);
        headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(headers, key: new byte[32]);
        _wrongKey = await Authenticate(headers);
        headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(headers);
        _duplicate = await Authenticate(headers, duplicate: true);
        headers[ForwardedIdentityForSpecs.SignatureHeader] = "intentionally-invalid";
        _disabled = await Authenticate(headers, enabled: false);
        headers.Remove(ForwardedIdentityForSpecs.SignatureHeader);
        _unsigned = await Authenticate(headers);
    }

    [Fact] void should_verify_the_real_listener_raw_target() => _valid.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_reenter_on_the_same_listener_request() => _reentered.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_reject_forged_identity() => _forged.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_expired_identity() => _expired.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_the_wrong_key() => _wrongKey.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_distinct_request_replay() => _replayed.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_repeated_raw_fields() => _duplicate.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_preserve_legacy_behavior_while_disabled() => _disabled.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_not_fall_back_to_unsigned_identity() => _unsigned.IsAuthenticated.ShouldBeFalse();

    async Task<AuthenticationResult> Authenticate(Dictionary<string, string> headers, bool enabled = true, bool duplicate = false, bool reenter = false)
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        var host = OperatingSystem.IsWindows() ? "localhost" : "127.0.0.1";
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://{host}:{port}/");
        listener.Start();
        using var client = new TcpClient();
        await client.ConnectAsync(host, port);
        var request = new StringBuilder($"POST /base/a%2fb?x=1 HTTP/1.1\r\nHost: {host}:{port}\r\nContent-Length: 0\r\n");
        foreach (var pair in headers)
        {
            request.Append(pair.Key).Append(": ").Append(pair.Value).Append("\r\n");
        }
        if (duplicate)
        {
            request.Append("x-ms-client-principal-name: n\r\n");
        }
        request.Append("\r\n");
        await client.GetStream().WriteAsync(Encoding.ASCII.GetBytes(request.ToString()));
        var received = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(10));
        await using var services = new ServiceCollection().BuildServiceProvider();
        var context = new HttpListenerRequestContext(received, services);
        var options = ForwardedIdentityForSpecs.Options();
        options.ForwardedIdentity.Enabled = enabled;
        var handler = new MicrosoftIdentityPlatformAuthenticationHandler(Options.Create(options), NullLoggerFactory.Instance);
        var result = await handler.HandleAuthentication(context);
        if (reenter)
        {
            _reentered = await handler.HandleAuthentication(context);
        }
        received.Response.Close();
        return result;
    }
}
