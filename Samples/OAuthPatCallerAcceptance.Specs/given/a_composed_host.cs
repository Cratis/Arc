// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;

namespace OAuthPatCallerAcceptance.Specs.given;

public class a_composed_host : Specification
{
    readonly Certificates _certificates = new();
    readonly EnvelopeSigner _signer = new();
    readonly int _port = FreePort();
    readonly int _defaultPort = FreePort();
    WebApplication? _app;
    HttpClient _client = null!;
    protected readonly SampleIdentityStore _store = new();
    protected TokenRecord _token = new();
    protected EnvelopeOptions _envelope = new();
    protected readonly List<TransportOutcome> _outcomes = [];
    protected bool _allowOAuth = true;
    protected bool _usePat;
    protected bool _raw;
    protected bool _defaultListener;
    protected bool _cached;
    protected bool _patOnly;
    protected bool _noCertificate;
    protected string? _literalToken;

    protected async Task SendBoth()
    {
        await Start();
        var token = await Token();
        await Send("/rest", HttpMethod.Get, token);
        await Send("/mcp", HttpMethod.Post, token);
    }

    protected async Task RequestPatIssuance()
    {
        await Start();
        await Send("/issue", HttpMethod.Post, await Token());
    }

    protected async Task Start()
    {
        var server = _certificates.Leaf(false);
        var peer = _noCertificate ? null : _certificates.Leaf(true);
        _client = new(new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, remote, _, _) => remote?.GetCertHashString(HashAlgorithmName.SHA256) == server.GetCertHashString(HashAlgorithmName.SHA256),
                ClientCertificates = peer is null ? [] : [peer]
            }
        }) { Timeout = TimeSpan.FromSeconds(10) };
        _app = Program.Build(
            new SampleSettings
            {
                CapabilityPort = _port, DefaultPort = _defaultPort, PublicKeyPem = _signer.PublicKeyPem,
                ServerCertificate = server, TrustedCertificateAuthoritiesPath = _certificates.Bundle,
                AllowOAuth = _allowOAuth, ResolveBeforeGate = _cached, PatOnly = _patOnly
            },
            _store);
        await _app.StartAsync();
    }

    protected async Task<string> Token()
    {
        if (_literalToken is not null) return _literalToken;
        if (!_usePat) return _store.Issue(_token);
        using var request = _signer.Request(Uri("/issue"), HttpMethod.Post, new() { Kind = "browser-session", CredentialId = "browser-session" });
        using var response = await _client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException($"Synthetic public PAT issuance failed with {(int)response.StatusCode}.");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _envelope = _envelope with { Kind = "pat", CredentialId = document.RootElement.GetProperty("credentialId").GetString()! };
        return document.RootElement.GetProperty("accessToken").GetString()!;
    }

    protected async Task Send(string path, HttpMethod method, string token)
    {
        if (path == "/rest" && _patOnly) path = "/pat-only";
        using var request = _raw ? Raw(Uri(path), method, token) : _signer.Request(Uri(path), method, _envelope with { AccessToken = token });
        if (method == HttpMethod.Post)
        {
            request.Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"read","arguments":{}}}""", Encoding.UTF8, "application/json");
            request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        }
        try
        {
            using var response = await _client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            var safeRefusal = RefusalBody.IsSafe(path, response.StatusCode, response.Content.Headers.ContentType?.MediaType, body);
            _outcomes.Add(new(path, response.StatusCode, response.IsSuccessStatusCode && body.Contains("persisted-person", StringComparison.Ordinal), safeRefusal));
        }
        catch (HttpRequestException) when (_noCertificate)
        {
            _outcomes.Add(new(path, null, false, true));
        }
    }

    async Task Destroy()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
        _client?.Dispose();
        _signer.Dispose();
        _certificates.Dispose();
    }

    Uri Uri(string path) => new($"{(_defaultListener ? "http" : "https")}://127.0.0.1:{(_defaultListener ? _defaultPort : _port)}{path}");

    HttpRequestMessage Raw(Uri uri, HttpMethod method, string token)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
        if (_envelope.IncludeSessionCookie) request.Headers.TryAddWithoutValidation("Cookie", "cratis-identity-session=synthetic-session");
        request.Headers.TryAddWithoutValidation("X-Tenant-ID", "tenant-a");
        return request;
    }

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
