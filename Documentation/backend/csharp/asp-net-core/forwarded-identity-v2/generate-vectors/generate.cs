// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#:property Nullable=enable
#:property TreatWarningsAsErrors=true
#:property PublishAot=false

using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
var mode = args.FirstOrDefault() ?? "--verify";
var selfTest = mode == "--self-test";
if (selfTest) mode = "--verify";
if (mode is not ("--verify" or "--regenerate"))
{
    Console.Error.WriteLine("Usage: dotnet run generate.cs -- [--verify|--regenerate|--self-test] [fixture.json]");
    return 2;
}
var path = args.Length > 1 ? args[1] : "../forwarded-identity-v2-vectors.json";
if (!File.Exists(path) && mode == "--verify" && args.Length > 0)
{
    Console.Error.WriteLine("Fixture not found; use --regenerate to create it.");
    return 2;
}
Fixture fixture;
if (mode == "--regenerate" || !File.Exists(path))
{
    var keys = File.Exists(path)
        ? JsonSerializer.Deserialize<Fixture>(File.ReadAllText(path), json)!.TestKeys
        : new[] { NewKey("signer", ["k1", "k1-alias"]), NewKey("wrong", ["k2"]) };
    fixture = Vectors.Generate(keys);
    File.WriteAllText(path, JsonSerializer.Serialize(fixture, json) + "\n", new UTF8Encoding(false));
}
else
{
    fixture = JsonSerializer.Deserialize<Fixture>(File.ReadAllText(path), json)!;
}
if (fixture.Cases.Count < 20 || fixture.Cases.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != fixture.Cases.Count)
{
    Console.Error.WriteLine("Fixture must contain at least 20 uniquely named cases.");
    return 2;
}
var groups = new Dictionary<string, NonceStore>(StringComparer.Ordinal);
var mismatches = 0;
foreach (var item in fixture.Cases)
{
    var store = new NonceStore();
    if (item.ReplayGroup is { } group)
    {
        if (!groups.TryGetValue(group, out var shared))
        {
            shared = new NonceStore();
            groups.Add(group, shared);
        }
        store = shared;
    }
    var actual = Reference.Verify(item, fixture.TestKeys, store);
    if (actual != item.Expected)
    {
        Console.Error.WriteLine($"MISMATCH {item.Name}: expected {item.Expected.Result}/{item.Expected.Reason}, got {actual.Result}/{actual.Reason}");
        mismatches++;
    }
    // Also compare the independently assembled canonical bytes on accepted cases.
    if (actual.Result == "accept" && item.SigningInput != Reference.CanonicalFor(item))
    {
        Console.Error.WriteLine($"MISMATCH {item.Name}: independent canonical bytes differ");
        mismatches++;
    }
}
Console.WriteLine($"{fixture.Cases.Count} cases; {mismatches} mismatches");
if (selfTest)
{
    var valid = fixture.Cases.Single(item => item.Name == "valid-v2");
    var poison = valid with { Envelope = valid.Envelope + "!" };
    var tampered = valid with { Inputs = valid.Inputs with { Audience = "urn:example:poison" } };
    if (Reference.Verify(valid, fixture.TestKeys, new NonceStore()).Result != "accept" ||
        Reference.Verify(poison, fixture.TestKeys, new NonceStore()) != new Outcome("reject", "format") ||
        Reference.Verify(tampered, fixture.TestKeys, new NonceStore()) != new Outcome("reject", "signature"))
    {
        Console.Error.WriteLine("Self-test failed to detect planted format and binding violations.");
        return 1;
    }
    Console.WriteLine("Self-test: accepted control and detected 2 planted violations.");
}
return mismatches == 0 ? 0 : 1;

static TestKey NewKey(string name, string[] keyIds)
{
    using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    return new(name, keyIds, key.ExportPkcs8PrivateKeyPem(), key.ExportSubjectPublicKeyInfoPem());
}

sealed record TestKey(string Name, string[] KeyIds, string PrivateKeyPem, string PublicKeyPem);
sealed record Header(string Name, string Value);
sealed record Outcome(string Result, string Reason);
sealed record Fixture(string Warning, string Format, TestKey[] TestKeys, List<Vector> Cases);
sealed record Inputs(
    string Method,
    string Path,
    string Audience,
    long Now,
    Header[] Headers,
    string TenantHeader = "x-example-tenant",
    bool ReplaySensitive = true,
    bool AuthenticatedTls = true,
    bool StoreAvailable = true,
    bool StoreFull = false,
    string Replica = "replica-a");
sealed record Vector(string Name, Inputs Inputs, string Envelope, string SigningInput, Outcome Expected, string? ReplayGroup = null);

static partial class Vectors
{
    [GeneratedRegex("%[a-fA-F0-9]{2}", RegexOptions.None, 100)]
    private static partial Regex PathEscapes();
    const long Now = 1700000000;
    const string Nonce = "AAECAwQFBgcICQoLDA0ODw";
    static readonly BigInteger Order = BigInteger.Parse("0FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551", NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    public static Fixture Generate(TestKey[] keys)
    {
        var principal = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"identityProvider\":\"test\",\"userId\":\"user-test\",\"userDetails\":\"Test User\",\"userRoles\":[\"user\"],\"claims\":[]}"));
        var input = new Inputs("POST", "/api/a%2fb//./task?x=1", "urn:example:app", Now,
        [
            new("x-ms-client-principal", principal), new("x-ms-client-principal-id", "user-test"),
            new("x-ms-client-principal-name", "Test User"), new("x-ms-client-principal-extension", "opaque"),
            new("x-cratis-token-client-id", "client-test"), new("x-cratis-token-scope", "read,write"),
            new("x-cratis-tenant-id", "tenant-test"), new("tenant-id", "tenant-test"),
            new("x-cratis-microservice", "service-test"), new("service-id", "service-test"),
            new("x-example-tenant", "tenant-test"), new("x-cratis-on-behalf-context", "opaque"),
            new("x-cratis-credential-kind", "access-token"), new("x-cratis-credential-id", "credential-test"),
            new("x-cratis-auth-time", "1699999900"), new("x-cratis-assurance", "urn:example:assurance:2"),
            new("x-cratis-actor", "actor-test"), new("x-cratis-policy-epoch", "7"),
            new("x-cratis-dpop-result", "verified")
        ]);
        var cases = new List<Vector>();
        var baseline = Signed("valid-v2", input, keys[0]);
        cases.Add(baseline);
        void Changed(string name, Inputs changed, string reason, Vector? original = null) =>
            cases.Add((original ?? baseline) with { Name = name, Inputs = changed, Expected = new("reject", reason) });
        void SignedChange(string name, Inputs changed, string reason) =>
            cases.Add(Signed(name, changed, keys[0]) with { Expected = new("reject", reason) });
        void Envelope(string name, string value, string reason) =>
            cases.Add(baseline with { Name = name, Envelope = value, Expected = new("reject", reason) });
        void Part(string name, int index, string replacement, string reason)
        {
            var parts = baseline.Envelope.Split('.');
            parts[index] = replacement;
            Envelope(name, string.Join('.', parts), reason);
        }

        // Every concrete covered header in the baseline has an isolated byte mutation.
        foreach (var header in input.Headers)
        {
            Changed("tamper-" + header.Name, Set(input, header.Name, header.Value + "!"), "signature");
        }
        foreach (var name in new[] { "x-cratis-on-behalf-actor", "x-cratis-on-behalf-assertion" })
        {
            var source = Signed("source", Set(input, name, "test-only-inner-material"), keys[0]);
            Changed("tamper-" + name, Set(source.Inputs, name, "changed"), "signature", source);
        }
        Changed("tamper-method", input with { Method = "PUT" }, "signature");
        Changed("tamper-path", input with { Path = "/api/changed" }, "signature");
        Changed("wrong-audience", input with { Audience = "urn:example:other" }, "signature");
        Part("tamper-known-kid-same-public-key", 1, "k1-alias", "signature");
        Part("unknown-kid", 1, "missing", "key");
        Part("tamper-timestamp-still-fresh", 2, "1700000001", "signature");
        Part("tamper-nonce", 3, "AQECAwQFBgcICQoLDA0ODw", "signature");
        var raw = Convert.FromBase64String(baseline.Envelope.Split('.')[4].Replace('-', '+').Replace('_', '/') + "==");
        raw[0] ^= 1;
        Part("tamper-signature", 4, Url(raw), "signature");
        cases.Add(Signed("known-kid-wrong-key-material", input, keys[1]) with { Expected = new("reject", "signature") });
        cases.Add(Signed("valid-key-rotation-overlap", input, keys[1], kid: "k2"));
        cases.Add(Signed("valid-kid-64-characters", input, keys[0], kid: new string('K', 64)));
        // This case explicitly provisions the boundary key ID in the reference key set.
        keys = [keys[0] with { KeyIds = [.. keys[0].KeyIds.Where(id => id.Length != 64), new string('K', 64)] }, keys[1]];

        cases.Add(Signed("timestamp-old-boundary-inclusive", input, keys[0], timestamp: Now - 60));
        cases.Add(Signed("timestamp-ahead-boundary-inclusive", input, keys[0], timestamp: Now + 30));
        cases.Add(Signed("timestamp-stale", input, keys[0], timestamp: Now - 61) with { Expected = new("reject", "freshness") });
        cases.Add(Signed("timestamp-too-far-ahead", input, keys[0], timestamp: Now + 31) with { Expected = new("reject", "freshness") });
        foreach (var stamp in new[] { "01700000000", "+1700000000", "-1", "1e9", "9223372036854775808" })
        {
            Part("timestamp-format-" + stamp, 2, stamp, "format");
        }
        Part("kid-empty", 1, "", "format");
        Part("kid-too-long", 1, new string('a', 65), "format");
        Part("kid-invalid-character", 1, "key!", "format");
        Part("nonce-padding", 3, Nonce + "==", "format");
        Part("nonce-too-short", 3, Url(new byte[15]), "format");
        Part("nonce-too-long", 3, Url(new byte[17]), "format");
        Part("nonce-noncanonical-unused-bits", 3, Nonce[..^1] + "x", "format");
        Part("nonce-non-base64url", 3, "+" + Nonce[1..], "format");
        Part("signature-padding", 4, baseline.Envelope.Split('.')[4] + "==", "format");
        Part("signature-too-short", 4, Url(new byte[63]), "format");
        Part("signature-too-long", 4, Url(new byte[65]), "format");
        var signatureText = baseline.Envelope.Split('.')[4];
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        Part("signature-noncanonical-unused-bits", 4, signatureText[..^1] + alphabet[alphabet.IndexOf(signatureText[^1]) + 1], "format");
        Part("signature-zero-scalars", 4, Url(new byte[64]), "signature");
        using (var signer = ECDsa.Create())
        {
            signer.ImportFromPem(keys[0].PrivateKeyPem);
            var der = signer.SignData(Encoding.UTF8.GetBytes(baseline.SigningInput), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
            Part("signature-der-rejected", 4, Url(der), "format");
        }
        // A genuine v1 HMAC envelope, not merely a v2 envelope with its version changed.
        // The v1 HMAC key is public TEST-ONLY bytes 00..1f, never production key material.
        var v1Input = input with { Headers = input.Headers.Where(h => !new[] { "x-cratis-credential-kind", "x-cratis-credential-id", "x-cratis-auth-time", "x-cratis-assurance", "x-cratis-actor", "x-cratis-policy-epoch", "x-cratis-dpop-result" }.Contains(h.Name)).ToArray() };
        var v1TestKey = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var v1Nonce = Url(v1TestKey);
        var v1Canonical = SignerCanonical(v1Input, "k1", Now.ToString(CultureInfo.InvariantCulture), v1Nonce)
            .Replace("Cratis.AuthProxy.ForwardedIdentity.v2\n", "Cratis.AuthProxy.ForwardedIdentity.v1\n", StringComparison.Ordinal);
        var v1Mac = Url(HMACSHA256.HashData(v1TestKey, Encoding.UTF8.GetBytes(v1Canonical)));
        cases.Add(new("v1-at-v2-only-backend", v1Input, $"v1.k1.{Now}.{v1Nonce}.{v1Mac}", v1Canonical, new("reject", "version")));
        Part("unknown-version", 0, "v3", "version");
        Envelope("missing-envelope", "", "format");
        Envelope("missing-segment", string.Join('.', baseline.Envelope.Split('.')[..4]), "format");
        Envelope("extra-segment", baseline.Envelope + ".extra", "format");
        Envelope("duplicate-envelope-host-values", baseline.Envelope + "," + baseline.Envelope, "format");
        Envelope("envelope-leading-ows", " " + baseline.Envelope, "format");
        Envelope("envelope-over-limit", new string('a', 257), "format");

        Changed("header-duplicate-case-variant", input with { Headers = [.. input.Headers, new("X-MS-CLIENT-PRINCIPAL-ID", "user-test")] }, "headers");
        Changed("covered-header-control", Set(input, "x-cratis-assurance", "value\nvalue"), "headers");
        Changed("covered-header-nonascii", Set(input, "x-cratis-assurance", "é"), "headers");
        Changed("method-lowercase", input with { Method = "post" }, "request");
        Changed("path-invalid-escape", input with { Path = "/api/%2" }, "request");
        Changed("path-absolute-form", input with { Path = "https://example.test/api" }, "request");
        Changed("path-fragment", input with { Path = "/api/#fragment" }, "request");
        Changed("path-nonascii", input with { Path = "/api/é" }, "request");
        Changed("path-decoded-slash-does-not-match", input with { Path = "/api/a/b//./task?x=1" }, "signature");
        Changed("path-double-escape-does-not-match", input with { Path = "/api/a%252Fb//./task?x=1" }, "signature");
        Changed("path-dot-resolution-does-not-match", input with { Path = "/api/a%2Fb/task?x=1" }, "signature");
        cases.Add(baseline with { Name = "path-escape-hex-case-normalized", Inputs = input with { Path = "/api/a%2Fb//./task?x=1" } });
        cases.Add(baseline with { Name = "query-change-outside-envelope", Inputs = input with { Path = "/api/a%2fb//./task?x=changed" } });
        cases.Add(baseline with { Name = "uncovered-header-change", Inputs = Set(input, "x-uncovered", "ignored") });
        cases.Add(baseline with { Name = "header-name-case-and-order-normalized", Inputs = input with { Headers = input.Headers.Reverse().Select(h => h with { Name = h.Name.ToUpperInvariant() }).ToArray() } });
        cases.Add(baseline with { Name = "outer-sp-and-htab-trimmed", Inputs = input with { Headers = input.Headers.Select(h => h with { Value = " \t" + h.Value + "\t " }).ToArray() } });
        var optional = input with { Headers = input.Headers.Where(h => !new[] { "x-cratis-auth-time", "x-cratis-assurance", "x-cratis-actor", "x-cratis-policy-epoch", "x-cratis-dpop-result" }.Contains(h.Name)).ToArray() };
        cases.Add(Signed("optional-v2-evidence-absent", optional, keys[0]));
        var absent = Signed("v1-optional-absent", input with { Headers = input.Headers.Where(h => h.Name != "service-id").ToArray() }, keys[0]);
        cases.Add(absent);
        cases.Add(Signed("v1-optional-present-empty", Set(absent.Inputs, "service-id", ""), keys[0]));
        Changed("absent-to-empty-is-covered-change", Set(absent.Inputs, "service-id", ""), "signature", absent);
        Changed("missing-required-principal-name", input with { Headers = input.Headers.Where(h => h.Name != "x-ms-client-principal-name").ToArray() }, "headers");
        Changed("empty-required-principal-name", Set(input, "x-ms-client-principal-name", ""), "headers");
        Changed("too-many-covered-headers", input with { Headers = [.. input.Headers, .. Enumerable.Range(0, 65).Select(i => new Header($"x-ms-client-principal-extra-{i}", "x"))] }, "bounds");
        Changed("canonical-bytes-over-limit", Set(input, "x-cratis-assurance", new string('a', 32768)), "bounds");
        foreach (var name in new[] { "x-cratis-credential-kind", "x-cratis-credential-id" })
        {
            SignedChange("missing-" + name, input with { Headers = input.Headers.Where(h => h.Name != name).ToArray() }, "metadata");
        }
        foreach (var name in input.Headers.Where(h => h.Name is "x-cratis-credential-kind" or "x-cratis-credential-id" or "x-cratis-auth-time" or "x-cratis-assurance" or "x-cratis-actor" or "x-cratis-policy-epoch" or "x-cratis-dpop-result").Select(h => h.Name))
        {
            SignedChange("empty-" + name, Set(input, name, ""), "metadata");
        }
        SignedChange("metadata-auth-time-leading-zero", Set(input, "x-cratis-auth-time", "01700000000"), "metadata");
        SignedChange("metadata-policy-epoch-overflow", Set(input, "x-cratis-policy-epoch", "9223372036854775808"), "metadata");
        SignedChange("metadata-credential-kind-case", Set(input, "x-cratis-credential-kind", "Access-Token"), "metadata");
        SignedChange("metadata-dpop-failure-not-signable", Set(input, "x-cratis-dpop-result", "failed"), "metadata");
        SignedChange("signed-subject-mismatch", Set(input, "x-ms-client-principal-id", "other-user"), "identity");
        SignedChange("signed-malformed-principal", Set(input, "x-ms-client-principal", "not-base64"), "identity");
        SignedChange("signed-null-roles", Set(input, "x-ms-client-principal", Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"userId\":\"user-test\",\"userRoles\":null,\"claims\":[]}"))), "identity");
        var unicodePrincipal = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"userId\":\"user-é\",\"userRoles\":[],\"claims\":[]}"));
        cases.Add(Signed("valid-rfc8187-subject-binding", Set(Set(input, "x-ms-client-principal", unicodePrincipal), "x-ms-client-principal-id", "UTF-8''user-%C3%A9"), keys[0]));
        SignedChange("invalid-rfc8187-subject", Set(input, "x-ms-client-principal-id", "UTF-8''user-%FF"), "identity");
        Changed("unauthenticated-hop", input with { AuthenticatedTls = false }, "hop");
        Changed("sensitive-store-unavailable", input with { StoreAvailable = false }, "store");
        Changed("sensitive-store-full", input with { StoreFull = true }, "store");
        cases.Add(baseline with { Name = "lower-risk-no-store-required", Inputs = input with { ReplaySensitive = false, StoreAvailable = false } });
        cases.Add(baseline with { Name = "fleet-first-reservation", ReplayGroup = "fleet" });
        cases.Add(baseline with { Name = "fleet-replay-other-replica-after-restart", ReplayGroup = "fleet", Inputs = input with { Replica = "replica-b-after-restart" }, Expected = new("reject", "replay") });
        cases.Add(baseline with { Name = "replay-retained-through-last-valid-second", ReplayGroup = "fleet", Inputs = input with { Now = Now + 60 }, Expected = new("reject", "replay") });
        cases.Add(baseline with { Name = "expired-envelope-even-after-store-cleanup", ReplayGroup = "fleet", Inputs = input with { Now = Now + 61 }, Expected = new("reject", "freshness") });
        cases.Add(baseline with { Name = "invalid-signature-does-not-reserve", ReplayGroup = "invalid", Inputs = Set(input, "x-cratis-credential-id", "changed"), Expected = new("reject", "signature") });
        cases.Add(baseline with { Name = "valid-after-invalid-signature", ReplayGroup = "invalid" });
        cases.Add(baseline with { Name = "lower-risk-first-use", ReplayGroup = "queries", Inputs = input with { ReplaySensitive = false } });
        cases.Add(baseline with { Name = "lower-risk-repeat-allowed", ReplayGroup = "queries", Inputs = input with { ReplaySensitive = false, Replica = "replica-b" } });
        var low = WithS(baseline, high: false);
        var high = WithS(baseline, high: true);
        cases.Add(low with { Name = "valid-low-s" });
        cases.Add(high with { Name = "valid-high-s" });
        cases.Add(low with { Name = "malleability-first-low-s", ReplayGroup = "s-pair" });
        cases.Add(high with { Name = "malleability-high-s-is-same-nonce-replay", ReplayGroup = "s-pair", Expected = new("reject", "replay") });
        return new("TEST ONLY: public private keys; NEVER deploy these keys or fixture credentials.", "Cratis.AuthProxy.ForwardedIdentity.v2", keys, cases);
    }

    static Inputs Set(Inputs input, string name, string value) => input with
    {
        Headers = [.. input.Headers.Where(h => !h.Name.Equals(name, StringComparison.OrdinalIgnoreCase)), new(name, value)]
    };

    static Vector Signed(string name, Inputs input, TestKey key, string kid = "k1", long timestamp = Now)
    {
        var stamp = timestamp.ToString(CultureInfo.InvariantCulture);
        var canonical = SignerCanonical(input, kid, stamp, Nonce);
        using var signer = ECDsa.Create();
        signer.ImportFromPem(key.PrivateKeyPem);
        var signature = signer.SignData(Encoding.UTF8.GetBytes(canonical), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return new(name, input, $"v2.{kid}.{stamp}.{Nonce}.{Url(signature)}", canonical, new("accept", "ok"));
    }

    // Producer implementation, deliberately not used by the reference verifier.
    static string SignerCanonical(Inputs input, string kid, string stamp, string nonce)
    {
        var exact = new[] { "x-cratis-token-client-id", "x-cratis-token-scope", "x-cratis-tenant-id", "tenant-id", "x-cratis-microservice", "service-id", input.TenantHeader.ToLowerInvariant(), "x-cratis-credential-kind", "x-cratis-credential-id", "x-cratis-auth-time", "x-cratis-assurance", "x-cratis-actor", "x-cratis-policy-epoch", "x-cratis-dpop-result" };
        var headers = input.Headers.Select(h => new Header(h.Name.ToLowerInvariant(), h.Value.Trim(' ', '\t')))
            .Where(h => h.Name != "x-cratis-forwarded-identity-signature" && (h.Name.StartsWith("x-ms-client-principal", StringComparison.Ordinal) || h.Name.StartsWith("x-cratis-on-behalf", StringComparison.Ordinal) || exact.Contains(h.Name)))
            .OrderBy(h => h.Name, StringComparer.Ordinal).ToArray();
        var path = input.Path.Split('?')[0];
        path = PathEscapes().Replace(path, match => match.Value.ToUpperInvariant());
        var result = new StringBuilder("Cratis.AuthProxy.ForwardedIdentity.v2\n");
        void Frame(string text) => result.Append(Encoding.UTF8.GetByteCount(text).ToString(CultureInfo.InvariantCulture)).Append(':').Append(text).Append('\n');
        foreach (var text in new[] { input.Audience, kid, stamp, nonce, input.Method, path, headers.Length.ToString(CultureInfo.InvariantCulture) }) Frame(text);
        foreach (var h in headers) { Frame(h.Name); Frame(h.Value); }
        return result.ToString();
    }

    static Vector WithS(Vector source, bool high)
    {
        var parts = source.Envelope.Split('.');
        var raw = Convert.FromBase64String(parts[4].Replace('-', '+').Replace('_', '/') + "==");
        var s = new BigInteger(raw.AsSpan(32), isUnsigned: true, isBigEndian: true);
        if ((s > Order / 2) != high) s = Order - s;
        raw.AsSpan(32).Clear();
        var bytes = s.ToByteArray(isUnsigned: true, isBigEndian: true);
        bytes.CopyTo(raw.AsSpan(64 - bytes.Length));
        parts[4] = Url(raw);
        return source with { Envelope = string.Join('.', parts) };
    }

    static string Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

sealed class Rejected(string reason) : Exception(reason);

sealed class NonceStore
{
    readonly Dictionary<(string Audience, string Kid, string Nonce), decimal> _reservations = [];

    public bool Reserve(string audience, string kid, string nonce, long timestamp, long now)
    {
        // Models one atomic insert and a shared durable store; not a production store implementation.
        foreach (var key in _reservations.Where(entry => entry.Value <= now).Select(entry => entry.Key).ToArray()) _reservations.Remove(key);
        return _reservations.TryAdd((audience, kid, nonce), (decimal)timestamp + 61);
    }
}

static partial class Reference
{
    [GeneratedRegex("\\A[A-Za-z0-9_-]{1,64}\\z", RegexOptions.None, 100)]
    private static partial Regex KeyIdPattern();

    [GeneratedRegex("\\A[A-Za-z0-9_-]+\\z", RegexOptions.None, 100)]
    private static partial Regex Base64UrlPattern();

    [GeneratedRegex("\\A[a-z][a-z0-9-]{0,63}\\z", RegexOptions.None, 100)]
    private static partial Regex CredentialKindPattern();
    static readonly HashSet<string> ExactHeaders = new(StringComparer.Ordinal)
    {
        "x-cratis-token-client-id", "x-cratis-token-scope", "x-cratis-tenant-id", "tenant-id", "x-cratis-microservice", "service-id",
        "x-cratis-credential-kind", "x-cratis-credential-id", "x-cratis-auth-time", "x-cratis-assurance", "x-cratis-actor", "x-cratis-policy-epoch", "x-cratis-dpop-result"
    };

    public static Outcome Verify(Vector vector, TestKey[] keys, NonceStore store)
    {
        try
        {
            var input = vector.Inputs;
            if (!input.AuthenticatedTls) throw new Rejected("hop");
            var envelope = Parse(vector.Envelope);
            if ((decimal)envelope.Timestamp < (decimal)input.Now - 60 || (decimal)envelope.Timestamp > (decimal)input.Now + 30) throw new Rejected("freshness");
            var key = keys.SingleOrDefault(candidate => candidate.KeyIds.Contains(envelope.Kid)) ?? throw new Rejected("key");
            var (canonical, headers) = Canonical(input, envelope.Parts);
            using var verifier = ECDsa.Create();
            verifier.ImportFromPem(key.PublicKeyPem);
            var verified = false;
            try { verified = verifier.VerifyData(canonical, envelope.Signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation); }
            catch (CryptographicException) { /* Invalid scalars are a uniform signature rejection. */ }
            if (!verified) throw new Rejected("signature");
            Principal(headers);
            Metadata(headers);
            if (input.ReplaySensitive)
            {
                if (!input.StoreAvailable || input.StoreFull) throw new Rejected("store");
                if (!store.Reserve(input.Audience, envelope.Kid, envelope.Parts[3], envelope.Timestamp, input.Now)) throw new Rejected("replay");
            }
            return new("accept", "ok");
        }
        catch (Rejected error) { return new("reject", error.Message); }
    }

    public static string CanonicalFor(Vector vector) => Encoding.UTF8.GetString(Canonical(vector.Inputs, Parse(vector.Envelope).Parts).Bytes);

    static (string[] Parts, string Kid, long Timestamp, byte[] Signature) Parse(string text)
    {
        if (text.Length is 0 or > 256 || text.Any(c => c <= 32 || c >= 127)) throw new Rejected("format");
        var parts = text.Split('.');
        if (parts.Length != 5) throw new Rejected("format");
        if (parts[0] != "v2") throw new Rejected("version");
        if (!KeyIdPattern().IsMatch(parts[1]) || !Unsigned(parts[2], out var timestamp)) throw new Rejected("format");
        _ = DecodeUrl(parts[3], 16);
        return (parts, parts[1], timestamp, DecodeUrl(parts[4], 64));
    }

    static byte[] DecodeUrl(string value, int size)
    {
        if (!Base64UrlPattern().IsMatch(value) || value.Length != (size * 8 + 5) / 6) throw new Rejected("format");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4)); }
        catch (FormatException) { throw new Rejected("format"); }
        var encoded = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        if (bytes.Length != size || encoded != value) throw new Rejected("format");
        return bytes;
    }

    // Consumer implementation: streaming frames, independent selection and path handling.
    static (byte[] Bytes, SortedDictionary<string, string> Headers) Canonical(Inputs input, string[] parts)
    {
        if (string.IsNullOrWhiteSpace(input.Audience) || input.Audience.Length > 128 || input.Audience.Any(c => c < 32 || c > 126)) throw new Rejected("request");
        const string methodToken = "!#$%&'*+-.^_`|~";
        if (input.Method.Length == 0 || input.Method.Any(c => !(c is >= 'A' and <= 'Z' or >= '0' and <= '9') && !methodToken.Contains(c))) throw new Rejected("request");
        if (!input.Path.StartsWith('/') || input.Path.Any(c => c < 33 || c > 126 || c == '#')) throw new Rejected("request");
        var query = input.Path.IndexOf('?');
        var path = (query < 0 ? input.Path : input.Path[..query]).ToCharArray();
        for (var i = 0; i < path.Length; i++)
        {
            if (path[i] != '%') continue;
            if (i + 2 >= path.Length || !Uri.IsHexDigit(path[i + 1]) || !Uri.IsHexDigit(path[i + 2])) throw new Rejected("request");
            path[++i] = char.ToUpperInvariant(path[i]);
            path[++i] = char.ToUpperInvariant(path[i]);
        }
        var headers = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var header in input.Headers)
        {
            var name = header.Name.ToLowerInvariant();
            if (name == "x-cratis-forwarded-identity-signature") continue;
            if (!name.StartsWith("x-ms-client-principal", StringComparison.Ordinal) && !name.StartsWith("x-cratis-on-behalf", StringComparison.Ordinal) && !ExactHeaders.Contains(name) && !name.Equals(input.TenantHeader, StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Length == 0 || name.Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9') && !methodToken.Contains(c))) throw new Rejected("headers");
            var value = header.Value.Trim(' ', '\t');
            if (value.Any(c => c < 32 || c > 126) || !headers.TryAdd(name, value)) throw new Rejected("headers");
        }
        if (headers.Count > 64) throw new Rejected("bounds");
        foreach (var required in new[] { "x-ms-client-principal", "x-ms-client-principal-id", "x-ms-client-principal-name" })
        {
            if (!headers.TryGetValue(required, out var value) || value.Length == 0) throw new Rejected("headers");
        }
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes("Cratis.AuthProxy.ForwardedIdentity.v2\n"));
        void Write(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            stream.Write(Encoding.ASCII.GetBytes(bytes.Length.ToString(CultureInfo.InvariantCulture) + ":"));
            stream.Write(bytes);
            stream.WriteByte(10);
            if (stream.Length > 32768) throw new Rejected("bounds");
        }
        foreach (var value in new[] { input.Audience, parts[1], parts[2], parts[3], input.Method, new string(path), headers.Count.ToString(CultureInfo.InvariantCulture) }) Write(value);
        foreach (var entry in headers) { Write(entry.Key); Write(entry.Value); }
        return (stream.ToArray(), headers);
    }

    static void Principal(SortedDictionary<string, string> headers)
    {
        try
        {
            var principalBytes = Convert.FromBase64String(headers["x-ms-client-principal"]);
            var text = new UTF8Encoding(false, true).GetString(principalBytes);
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (!root.TryGetProperty("userId", out var subject) || subject.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(subject.GetString()) ||
                !root.TryGetProperty("userRoles", out var roles) || roles.ValueKind != JsonValueKind.Array || roles.EnumerateArray().Any(role => role.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(role.GetString())) ||
                !root.TryGetProperty("claims", out var claims) || claims.ValueKind != JsonValueKind.Array || claims.EnumerateArray().Any(claim => claim.ValueKind != JsonValueKind.Object || !claim.TryGetProperty("typ", out var type) || type.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(type.GetString()) || !claim.TryGetProperty("val", out var value) || value.ValueKind != JsonValueKind.String)) throw new Rejected("identity");
            var id = headers["x-ms-client-principal-id"];
            if (id.StartsWith("UTF-8''", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = new List<byte>();
                for (var i = 7; i < id.Length; i++)
                {
                    if (id[i] != '%') { bytes.Add((byte)id[i]); continue; }
                    if (i + 2 >= id.Length || !Uri.IsHexDigit(id[i + 1]) || !Uri.IsHexDigit(id[i + 2])) throw new Rejected("identity");
                    bytes.Add(Convert.ToByte(id.Substring(i + 1, 2), 16));
                    i += 2;
                }
                id = new UTF8Encoding(false, true).GetString(bytes.ToArray());
            }
            if (id != subject.GetString()) throw new Rejected("identity");
        }
        catch (Exception error) when (error is FormatException or JsonException or DecoderFallbackException or InvalidOperationException) { throw new Rejected("identity"); }
    }

    static void Metadata(SortedDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("x-cratis-credential-kind", out var kind) || !CredentialKindPattern().IsMatch(kind) ||
            !headers.TryGetValue("x-cratis-credential-id", out var id) || id.Length == 0) throw new Rejected("metadata");
        foreach (var name in new[] { "x-cratis-auth-time", "x-cratis-assurance", "x-cratis-actor", "x-cratis-policy-epoch", "x-cratis-dpop-result" })
        {
            if (!headers.TryGetValue(name, out var value)) continue;
            if (value.Length == 0) throw new Rejected("metadata");
            if (name is "x-cratis-auth-time" or "x-cratis-policy-epoch" && !Unsigned(value, out _)) throw new Rejected("metadata");
            if (name == "x-cratis-dpop-result" && value is not ("verified" or "not-applicable")) throw new Rejected("metadata");
        }
    }

    static bool Unsigned(string value, out long number) => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number) && value.Length <= 19 && value == number.ToString(CultureInfo.InvariantCulture);
}
