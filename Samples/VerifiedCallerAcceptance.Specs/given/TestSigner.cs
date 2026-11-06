// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VerifiedCallerAcceptance.Specs.given;

public sealed class TestSigner : IDisposable
{
    readonly ECDsa _key = ECDsa.Create();
    readonly Dictionary<string, string> _headers;
    readonly JsonElement _vector;

    public TestSigner()
    {
        using var vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "forwarded-identity-v2-vectors.json")));
        var key = vectors.RootElement.GetProperty("testKeys")[0];
        var valid = vectors.RootElement.GetProperty("cases").EnumerateArray().Single(value => value.GetProperty("name").GetString() == "valid-v2");
        _vector = valid.Clone();
        _key.ImportFromPem(key.GetProperty("privateKeyPem").GetString()!);
        PublicKeyPem = key.GetProperty("publicKeyPem").GetString()!;
        KeyId = key.GetProperty("keyIds")[0].GetString()!;
        var browserHeaders = new HashSet<string>(StringComparer.Ordinal)
        {
            "x-ms-client-principal", "x-ms-client-principal-id", "x-ms-client-principal-name", "x-cratis-credential-id", "x-cratis-issuer", "x-cratis-subject", "x-cratis-idp", "x-cratis-auth-time", "x-cratis-acr"
        };
        _headers = valid.GetProperty("inputs").GetProperty("headers").EnumerateArray()
            .Where(header => browserHeaders.Contains(header.GetProperty("name").GetString()!))
            .ToDictionary(header => header.GetProperty("name").GetString()!, header => header.GetProperty("value").GetString()!, StringComparer.Ordinal);
        _headers["x-cratis-credential-kind"] = "browser-session";
        _headers["x-cratis-sender-binding"] = "none";
    }

    public string PublicKeyPem { get; }
    public string KeyId { get; }

    public HttpRequestMessage Request(int port, string method, string target, RequestOptions? options = null)
    {
        options ??= new();
        var headers = new SortedDictionary<string, string>(_headers, StringComparer.Ordinal)
        {
            ["x-ms-client-principal-id"] = options.Subject,
            ["x-ms-client-principal"] = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
            {
                identityProvider = "https://login.example.test",
                userId = options.Subject,
                userDetails = "Test User",
                userRoles = new[] { "user" },
                claims = Array.Empty<object>()
            })),
            ["x-cratis-subject"] = options.Subject,
            ["x-cratis-token-scope"] = options.Scope,
            [VerifiedCallerAcceptanceSettings.TenantHeader] = options.Tenant
        };
        if (string.IsNullOrEmpty(options.Scope)) headers.Remove("x-cratis-token-scope");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var nonce = options.Nonce ?? Base64Url(RandomNumberGenerator.GetBytes(16));
        var keyId = options.KeyId ?? KeyId;
        var input = Canonical(options.Audience, keyId, timestamp, nonce, method, target, headers);
        var signature = Base64Url(_key.SignData(Encoding.UTF8.GetBytes(input), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        var request = new HttpRequestMessage(new HttpMethod(method), $"http://127.0.0.1:{port}{target}");
        foreach (var header in headers) request.Headers.Add(header.Key, header.Value);
        if (!options.OmitSignature) request.Headers.Add("x-cratis-forwarded-identity-signature", $"v2.{keyId}.{timestamp}.{nonce}.{signature}");
        if (method == "POST") request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        return request;
    }

    public (bool InputMatches, bool SignatureVerifies) CheckSharedVector()
    {
        var inputs = _vector.GetProperty("inputs");
        var parts = _vector.GetProperty("envelope").GetString()!.Split('.');
        var headers = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var header in inputs.GetProperty("headers").EnumerateArray())
        {
            headers.Add(header.GetProperty("name").GetString()!.ToLowerInvariant(), header.GetProperty("value").GetString()!.Trim(' ', '\t'));
        }
        var target = inputs.GetProperty("path").GetString()!;
        var input = Canonical(inputs.GetProperty("aud").GetString()!, parts[1], parts[2], parts[3], inputs.GetProperty("method").GetString()!, target, headers);
        var bytes = Encoding.UTF8.GetBytes(_vector.GetProperty("signingInput").GetString()!);
        var encodedSignature = parts[4].Replace('-', '+').Replace('_', '/');
        var signature = Convert.FromBase64String(encodedSignature.PadRight((encodedSignature.Length + 3) / 4 * 4, '='));
        using var publicKey = ECDsa.Create();
        publicKey.ImportFromPem(PublicKeyPem);
        return (input == _vector.GetProperty("signingInput").GetString(), publicKey.VerifyData(bytes, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
    }

    public void Dispose() => _key.Dispose();

    static string Canonical(string audience, string keyId, string timestamp, string nonce, string method, string target, SortedDictionary<string, string> headers)
    {
        var input = new StringBuilder("Cratis.AuthProxy.ForwardedIdentity.v2\n");
        foreach (var value in new[] { audience, keyId, timestamp, nonce, method, CanonicalPath(target), headers.Count.ToString(CultureInfo.InvariantCulture) }) Frame(input, value);
        foreach (var header in headers)
        {
            Frame(input, header.Key);
            Frame(input, header.Value);
        }
        return input.ToString();
    }

    static string CanonicalPath(string target)
    {
        var path = target.Split('?')[0].ToCharArray();
        for (var index = 0; index < path.Length; index++)
        {
            if (path[index] != '%') continue;
            path[++index] = char.ToUpperInvariant(path[index]);
            path[++index] = char.ToUpperInvariant(path[index]);
        }
        return new(path);
    }

    static void Frame(StringBuilder input, string value) => input.Append(Encoding.UTF8.GetByteCount(value).ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('\n');
    static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
