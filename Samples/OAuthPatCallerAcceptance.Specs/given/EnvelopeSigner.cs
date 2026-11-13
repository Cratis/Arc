// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OAuthPatCallerAcceptance.Specs.given;

public sealed class EnvelopeSigner : IDisposable
{
    readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public string PublicKeyPem => _key.ExportSubjectPublicKeyInfoPem();

    public HttpRequestMessage Request(Uri uri, HttpMethod method, EnvelopeOptions options)
    {
        var credentialInUri = options.RetiredCredentialInUri ? "dpat_synthetic-retired-credential" : "synthetic-uri-credential";
        var target = options.CredentialInUri || options.RetiredCredentialInUri ? new Uri(uri + "?access_token=" + credentialInUri) : uri;
        var request = new HttpRequestMessage(method, target);
        var headers = Headers(options);
        foreach (var header in headers) request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        var signature = Sign(request, headers, options);
        if (options.UnknownKey) signature = signature.Replace("sample-key", "unknown-key", StringComparison.Ordinal);
        if (options.CorruptSignature) signature = signature[..^1] + (signature[^1] == 'A' ? "B" : "A");
        if (!options.OmitSignature) request.Headers.TryAddWithoutValidation("x-cratis-forwarded-identity-signature", signature);
        if (options.IncludeSessionCookie) request.Headers.TryAddWithoutValidation("Cookie", ".Cratis.Session=synthetic-session");
        if (options.UnsignedTenantOverride) request.Headers.TryAddWithoutValidation("X-Cratis-Tenant-Id", "unsigned-tenant");
        if (options.AccessToken is not null && !options.OmitBearer)
        {
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + options.AccessToken);
            if (options.DuplicateAuthorization) request.Headers.TryAddWithoutValidation("Authorization", "Bearer another-synthetic-token");
        }
        return request;
    }

    public void Dispose() => _key.Dispose();

    static SortedDictionary<string, string> Headers(EnvelopeOptions options)
    {
        var headers = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["x-ms-client-principal"] = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
            {
                identityProvider = "https://idp.example.test", userId = SampleSettings.Subject, userDetails = "Synthetic Person", userRoles = new[] { "user" }, claims = Array.Empty<object>()
            })),
            ["x-ms-client-principal-id"] = SampleSettings.Subject,
            ["x-ms-client-principal-name"] = "Synthetic Person",
            ["x-cratis-credential-kind"] = options.Kind,
            ["x-cratis-credential-id"] = options.CredentialId,
            ["x-cratis-issuer"] = SampleSettings.Issuer,
            ["x-cratis-subject"] = SampleSettings.Subject,
            ["x-cratis-idp"] = "https://idp.example.test",
            ["x-cratis-auth-time"] = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            ["x-cratis-acr"] = "basic",
            ["x-cratis-sender-binding"] = "none",
            ["x-cratis-token-scope"] = "read",
            ["x-cratis-token-client-id"] = "sample-client",
            ["x-tenant-id"] = "tenant-a"
        };
        if (options.AccessToken is not null) headers["x-cratis-forwarded-token-sha256"] = Url(SHA256.HashData(Encoding.UTF8.GetBytes(options.AccessToken)));
        foreach (var header in options.Headers)
        {
            if (header.Value is null) headers.Remove(header.Key);
            else headers[header.Key] = header.Value;
        }
        return headers;
    }

    string Sign(HttpRequestMessage request, SortedDictionary<string, string> headers, EnvelopeOptions options)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var nonce = options.Nonce ?? Url(RandomNumberGenerator.GetBytes(16));
        var input = new StringBuilder("Cratis.AuthProxy.ForwardedIdentity.v2\n");
        foreach (var value in new[] { options.Audience, "sample-key", timestamp, nonce, request.Method.Method, request.RequestUri!.AbsolutePath, headers.Count.ToString(CultureInfo.InvariantCulture) }) Frame(input, value);
        foreach (var header in headers)
        {
            Frame(input, header.Key);
            Frame(input, header.Value);
        }
        var signature = Url(_key.SignData(Encoding.UTF8.GetBytes(input.ToString()), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        return $"{(options.LegacySignature ? "v1" : "v2")}.sample-key.{timestamp}.{nonce}.{signature}";
    }

    static void Frame(StringBuilder input, string value) => input.Append(Encoding.UTF8.GetByteCount(value).ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('\n');
    static string Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
