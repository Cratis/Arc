// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.given;

internal static class VerificationFixture
{
    internal static ForwardedIdentityV2Settings Settings(Inputs input) => new(input.Aud, "resource", input.RealmIssuer, input.TenantHeader, ForwardedIdentityV2Vectors.Fixture.TestKeys.SelectMany(key => key.KeyIds.Select(id => new KeyValuePair<string, ForwardedIdentityV2PublicKey>(id, ForwardedIdentityV2PublicKey.FromPem(id, key.PublicKeyPem)))).ToDictionary(), false);

    internal static ForwardedIdentityV2Request Request(Vector vector) => new(vector.Inputs.Method, vector.Inputs.Path, vector.Inputs.Headers.Select(header => new ForwardedIdentityV2Header(header.Name, header.Value)).Append(new(ForwardedIdentityV2Envelope.Header, vector.Envelope)).ToArray(), vector.Inputs.ReplaySensitive, vector.Inputs.AuthenticatedTls);

    internal static Vector Sign(Vector vector)
    {
        var parts = vector.Envelope.Split('.');
        var bytes = ForwardedIdentityV2Canonicalization.Create(Settings(vector.Inputs), Request(vector), parts).Bytes;
        using var key = ECDsa.Create();
        key.ImportFromPem(ForwardedIdentityV2Vectors.Fixture.TestKeys.Single(key => key.KeyIds.Contains(parts[1])).PrivateKeyPem);
        parts[4] = Url(key.SignData(bytes, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        return vector with { Envelope = string.Join('.', parts), SigningInput = Encoding.UTF8.GetString(bytes) };
    }

    internal static Vector Header(Vector vector, string name, string? value, bool sign = true)
    {
        var headers = vector.Inputs.Headers.Where(header => !header.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (value is not null) headers.Add(new(name, value));
        var changed = vector with { Inputs = vector.Inputs with { Headers = headers.ToArray() } };
        return sign ? Sign(changed) : changed;
    }

    internal static Vector DirectBrowser()
    {
        var vector = ForwardedIdentityV2Vectors.Named("credential-kind-browser-session");
        vector = Header(vector, "x-cratis-act", null, false);
        return Header(vector, "x-cratis-grant-id", null);
    }

    internal static string Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    internal static string Base64(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    internal static ForwardedIdentityV2Verifier Verifier(Vector vector, IForwardedIdentityNonceStore? store = null, IForwardedBearerVerifier? bearers = null) => new(store ?? new ProcessLocalForwardedIdentityNonceStore(), bearers ?? new VectorBearerVerifier(vector.Inputs.VerifiedBearer), new FixedTime(vector.Inputs.Now));

    internal static IVerifiedCredentialSource Source(Vector vector, IForwardedIdentityNonceStore? store = null, ICallerPersonDirectory? persons = null, IForwardedBearerVerifier? bearers = null, bool v2Only = false)
    {
        var accessor = Substitute.For<IHttpRequestContextAccessor>();
        var request = RawRequest(vector);
        accessor.Current.Returns(request);
        var original = Settings(vector.Inputs);
        var settings = new ForwardedIdentityV2Settings(original.Audience, original.Resource, original.RealmIssuer, original.TenantHeader, original.Keys, v2Only);
        return new ForwardedIdentityV2CredentialSource(accessor, settings, Verifier(vector, store, bearers), new("resource", "test"), persons ?? new PersonDirectory(), NullLogger<ForwardedIdentityV2CredentialSource>.Instance);
    }

    internal static CredentialPresentation Presentation(Vector vector) => new(vector.Inputs.Headers.Where(header => header.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).Select(header => header.Value).ToArray(), new Dictionary<string, IReadOnlyList<string>> { [ForwardedIdentityV2Envelope.Header] = [vector.Envelope] }, shape: PresentedCredentialShape.Envelope);

    internal static IHttpRequestContext RawRequest(Vector vector)
    {
        var request = (IHttpRequestContext)Substitute.For([typeof(IHttpRequestContext), typeof(ICanReadForwardedIdentityRequest), typeof(ICanPreserveCallerCredentialHeaders), typeof(ICanReadAuthenticatedForwardingHop)], []);
        ((ICanReadAuthenticatedForwardingHop)request).IsAuthenticatedForwardingHop.Returns(vector.Inputs.AuthenticatedTls);
        var raw = (ICanReadForwardedIdentityRequest)request;
        var snapshot = Request(vector);
        request.Items.Returns(new Dictionary<object, object?>());
        request.Method.Returns(snapshot.Method);
        raw.RawTarget.Returns(snapshot.RawTarget);
        raw.HeaderNames.Returns(snapshot.Headers.Select(header => header.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        raw.GetHeaderValues(Arg.Any<string>()).Returns(call => snapshot.Headers.Where(header => header.Name.Equals(call.Arg<string>(), StringComparison.OrdinalIgnoreCase)).Select(header => header.Value).ToArray());
        ((ICanPreserveCallerCredentialHeaders)request).PreservesDuplicateCredentialHeaders.Returns(true);
        return request;
    }
}
