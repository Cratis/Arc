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
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
var mode = args.FirstOrDefault() ?? "--verify";
var selfTest = mode == "--self-test";
var exportIdentity = mode == "--export-identity";
if (selfTest || exportIdentity) mode = "--verify";
if (exportIdentity && args.Length != 3)
{
    Console.Error.WriteLine("Usage: --export-identity fixture.json output-directory");
    return 2;
}
if (mode is not ("--verify" or "--regenerate"))
{
    Console.Error.WriteLine("Usage: dotnet run generate.cs -- [--verify|--regenerate|--self-test|--export-identity] [fixture.json] [output-directory]");
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
    var previous = File.Exists(path) ? JsonSerializer.Deserialize<Fixture>(File.ReadAllText(path), json) : null;
    fixture = Vectors.Generate(keys);
    // Preserve an existing signature whenever its canonical input and signing operation did not change.
    if (previous is not null) fixture = Vectors.KeepUnchangedSignatures(fixture, previous);
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
    var producer = Vectors.CanonicalResult(item.Inputs);
    var consumer = Reference.CanonicalResult(item.Inputs);
    if (producer != consumer)
    {
        Console.Error.WriteLine($"MISMATCH {item.Name}: independent canonicalization outcomes differ");
        mismatches++;
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
    var tampered = valid with { Inputs = valid.Inputs with { Aud = "other-api" } };
    var bound = fixture.Cases.Single(item => item.Name == "matching-forwarded-bearer");
    var swappedBearer = bound with { Inputs = bound.Inputs with { Headers = bound.Inputs.Headers.Select(h => h.Name == "Authorization" ? h with { Value = "Bearer TEST-ONLY-swapped-token" } : h).ToArray() } };
    var pat = fixture.Cases.Single(item => item.Name == "pat-bearer-caller-from-local-pat-verifier");
    var refusedPat = pat with { Inputs = pat.Inputs with { VerifiedBearer = pat.Inputs.VerifiedBearer! with { Accepted = false } } };
    var wrongAccount = bound with { Inputs = bound.Inputs with { VerifiedBearer = bound.Inputs.VerifiedBearer! with { AccountKey = new("https://other.example.test", bound.Inputs.VerifiedBearer!.AccountKey.Subject) } } };
    if (Reference.Verify(valid, fixture.TestKeys, new NonceStore()).Result != "accept" ||
        Reference.Verify(bound, fixture.TestKeys, new NonceStore()) != new Outcome("accept", "ok", "oauth-verifier") ||
        Reference.Verify(pat, fixture.TestKeys, new NonceStore()) != new Outcome("accept", "ok", "pat-verifier") ||
        Reference.Verify(refusedPat, fixture.TestKeys, new NonceStore()) != new Outcome("reject", "credential") ||
        Reference.Verify(wrongAccount, fixture.TestKeys, new NonceStore()) != new Outcome("reject", "credential") ||
        Reference.Verify(swappedBearer, fixture.TestKeys, new NonceStore()) != new Outcome("reject", "credential") ||
        Reference.Verify(poison, fixture.TestKeys, new NonceStore()) != new Outcome("reject", "format") ||
        Reference.Verify(tampered, fixture.TestKeys, new NonceStore()) != new Outcome("reject", "signature"))
    {
        Console.Error.WriteLine("Self-test failed to accept controls or detect planted format, canonical binding, bearer binding and bearer verification violations.");
        return 1;
    }
    Console.WriteLine("Self-test: accepted 3 controls and detected 5 planted violations.");
}
if (exportIdentity && mismatches == 0) IdentityDelivery.Export(fixture, args[2], json);
return mismatches == 0 ? 0 : 1;

static TestKey NewKey(string name, string[] keyIds)
{
    using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    return new(name, keyIds, key.ExportPkcs8PrivateKeyPem(), key.ExportSubjectPublicKeyInfoPem());
}

sealed record TestKey(string Name, string[] KeyIds, string PrivateKeyPem, string PublicKeyPem);
sealed record Header(string Name, string Value);
sealed record Outcome(string Result, string Reason, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CallerSource = null);
sealed record AccountKey(string Issuer, string Subject);
sealed record VerifiedBearerResult(string Verifier, AccountKey AccountKey, bool Accepted = true);
sealed record Fixture(string Warning, string Format, TestKey[] TestKeys, List<Vector> Cases);
sealed record Inputs(
    string Method,
    string Path,
    string Aud,
    long Now,
    Header[] Headers,
    string TenantHeader = "x-example-tenant",
    bool ReplaySensitive = true,
    bool AuthenticatedTls = true,
    bool StoreAvailable = true,
    bool StoreFull = false,
    string Replica = "replica-a",
    string RealmIssuer = "https://identity.example.test",
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] VerifiedBearerResult? VerifiedBearer = null);
sealed record Vector(string Name, Inputs Inputs, string Envelope, string SigningInput, Outcome Expected, string? ReplayGroup = null);
sealed record CanonicalCheck(string? Bytes, string? Error);

static partial class Vectors
{
    [GeneratedRegex("%[a-fA-F0-9]{2}", RegexOptions.None, 100)]
    private static partial Regex PathEscapes();
    static readonly string[] ContextHeaders = ["x-cratis-credential-kind", "x-cratis-credential-id", "x-cratis-issuer", "x-cratis-subject", "x-cratis-idp", "x-cratis-auth-time", "x-cratis-acr", "x-cratis-amr", "x-cratis-act", "x-cratis-grant-id", "x-cratis-policy-epoch", "x-cratis-sender-binding", "x-cratis-forwarded-token-sha256"];
    static readonly string[] RequiredPrincipalHeaders = ["x-ms-client-principal", "x-ms-client-principal-id", "x-ms-client-principal-name"];
    const long Now = 1700000000;
    const string Nonce = "AAECAwQFBgcICQoLDA0ODw";
    static readonly BigInteger Order = BigInteger.Parse("0FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551", NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    public static Fixture Generate(TestKey[] keys)
    {
        const string subject = "8b167ad2-0f89-4c78-9d42-6d4580495d32";
        var principal = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"identityProvider\":\"https://login.example.test\",\"userId\":\"{subject}\",\"userDetails\":\"Test User\",\"userRoles\":[\"user\"],\"claims\":[]}}"));
        var input = new Inputs("POST", "/api/a%2fb//./task?x=1", "example-api", Now,
        [
            new("x-ms-client-principal", principal), new("x-ms-client-principal-id", subject),
            new("x-ms-client-principal-name", "Test User"), new("x-ms-client-principal-extension", "opaque,extension"),
            new("x-cratis-token-client-id", "client-test"), new("x-cratis-token-scope", "example:read example:write"),
            new("x-cratis-tenant-id", "tenant-test"), new("tenant-id", "tenant-test"),
            new("x-cratis-microservice", "service-test"), new("service-id", "service-test"),
            new("x-example-tenant", "tenant-test"), new("x-cratis-on-behalf-context", "opaque"),
            new("x-cratis-credential-kind", "oauth-grant"), new("x-cratis-credential-id", "credential-test"),
            new("x-cratis-issuer", "https://identity.example.test"), new("x-cratis-subject", subject), new("x-cratis-idp", "https://login.example.test"),
            new("x-cratis-auth-time", "1699999900"), new("x-cratis-acr", "basic"),
            new("x-cratis-amr", Base64Json("[\"pwd\"]")),
            new("x-cratis-act", Base64Json("{\"iss\":\"https://actors.example.test\",\"sub\":\"agent-7\"}")),
            new("x-cratis-grant-id", "grant-test"), new("x-cratis-policy-epoch", "7"),
            new("x-cratis-sender-binding", Base64Json("{\"jkt\":\"AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8\",\"jti\":\"proof-test\"}"))
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
        Changed("wrong-aud", input with { Aud = "other-api" }, "signature");
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
        var v1Input = input with { Headers = input.Headers.Where(h => !ContextHeaders.Contains(h.Name)).ToArray() };
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
        Changed("covered-header-control", Set(input, "x-cratis-acr", "value\nvalue"), "headers");
        Changed("covered-header-nonascii", Set(input, "x-cratis-acr", "é"), "headers");
        Changed("covered-header-invalid-name", Set(input, "x-ms-client-principal-bad name", "x"), "headers");
        Changed("covered-header-unicode-name", Set(input, "x-ms-client-principal-\u212A", "x"), "headers");
        Changed("aud-is-not-resource-indicator", input with { Aud = "https://api.example.test" }, "request");
        Changed("aud-invalid-case", input with { Aud = "Example-api" }, "request");
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
        var optional = input with { Headers = input.Headers.Where(h => !new[] { "x-cratis-amr", "x-cratis-act", "x-cratis-grant-id", "x-cratis-policy-epoch" }.Contains(h.Name)).ToArray() };
        optional = Set(optional, "x-cratis-sender-binding", "none");
        cases.Add(Signed("optional-v2-evidence-absent", optional, keys[0]));
        var absent = Signed("v1-optional-absent", input with { Headers = input.Headers.Where(h => h.Name != "service-id").ToArray() }, keys[0]);
        cases.Add(absent);
        cases.Add(Signed("v1-optional-present-empty", Set(absent.Inputs, "service-id", ""), keys[0]));
        Changed("absent-to-empty-is-covered-change", Set(absent.Inputs, "service-id", ""), "signature", absent);
        Changed("missing-required-principal-name", input with { Headers = input.Headers.Where(h => h.Name != "x-ms-client-principal-name").ToArray() }, "headers");
        Changed("empty-required-principal-name", Set(input, "x-ms-client-principal-name", ""), "headers");
        Changed("too-many-covered-headers", input with { Headers = [.. input.Headers, .. Enumerable.Range(0, 65).Select(i => new Header($"x-ms-client-principal-extra-{i}", "x"))] }, "bounds");
        Changed("canonical-bytes-over-limit", Set(input, "x-cratis-acr", new string('a', 32768)), "bounds");
        foreach (var name in new[] { "x-cratis-credential-kind", "x-cratis-credential-id", "x-cratis-issuer", "x-cratis-subject", "x-cratis-idp", "x-cratis-auth-time", "x-cratis-acr", "x-cratis-sender-binding", "x-cratis-grant-id" })
        {
            SignedChange("missing-" + name, input with { Headers = input.Headers.Where(h => h.Name != name).ToArray() }, "metadata");
        }
        foreach (var name in ContextHeaders.Where(name => name != "x-cratis-forwarded-token-sha256"))
        {
            SignedChange("empty-" + name, Set(input, name, ""), "metadata");
        }
        SignedChange("metadata-auth-time-leading-zero", Set(input, "x-cratis-auth-time", "01700000000"), "metadata");
        SignedChange("metadata-policy-epoch-overflow", Set(input, "x-cratis-policy-epoch", "9223372036854775808"), "metadata");
        SignedChange("metadata-credential-kind-case", Set(input, "x-cratis-credential-kind", "OAuth-Grant"), "metadata");
        SignedChange("metadata-access-token-is-not-credential-kind", Set(input, "x-cratis-credential-kind", "access-token"), "metadata");
        SignedChange("metadata-sender-binding-flag-not-evidence", Set(input, "x-cratis-sender-binding", "verified"), "metadata");
        SignedChange("metadata-sender-binding-missing-proof-jti", Set(input, "x-cratis-sender-binding", Base64Json("{\"jkt\":\"AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8\"}")), "metadata");
        SignedChange("metadata-sender-binding-invalid-thumbprint", Set(input, "x-cratis-sender-binding", Base64Json("{\"jkt\":\"invalid\",\"jti\":\"proof-test\"}")), "metadata");
        SignedChange("metadata-amr-not-array", Set(input, "x-cratis-amr", Base64Json("\"pwd\"")), "metadata");
        SignedChange("metadata-act-not-object", Set(input, "x-cratis-act", Base64Json("\"actor-test\"")), "metadata");
        SignedChange("metadata-act-missing-issuer", Set(input, "x-cratis-act", Base64Json("{\"sub\":\"agent-7\"}")), "metadata");
        SignedChange("metadata-untrusted-realm-issuer", Set(input, "x-cratis-issuer", "https://other.example.test"), "metadata");
        SignedChange("metadata-negative-policy-epoch", Set(input, "x-cratis-policy-epoch", "-1"), "metadata");
        var rfcSubject = Set(input, "x-ms-client-principal-id", "UTF-8''" + subject);
        cases.Add(Signed("valid-rfc8187-uuid-subject-binding", rfcSubject, keys[0]));
        SignedChange("signed-subject-uppercase", Set(Set(input, "x-ms-client-principal", Base64Json($"{{\"userId\":\"{subject.ToUpperInvariant()}\",\"userRoles\":[],\"claims\":[]}}")), "x-ms-client-principal-id", subject.ToUpperInvariant()), "identity");
        cases.Add(Signed("assurance-auth-time-integer-numericdate", Set(input, "x-cratis-auth-time", "-1"), keys[0]));
        SignedChange("assurance-auth-time-fraction-refused", Set(input, "x-cratis-auth-time", "1699999900.5"), "metadata");
        SignedChange("signed-account-key-subject-mismatch", Set(input, "x-cratis-subject", "091ee384-c4c8-4a09-b278-47aad8b8a8b2"), "identity");
        foreach (var kind in new[] { "browser-session", "oauth-grant", "pat", "on-behalf-session", "machine-principal", "workload" })
        {
            var classified = Set(input, "x-cratis-credential-kind", kind);
            if (kind is "machine-principal" or "workload")
            {
                classified = classified with { Headers = classified.Headers.Where(h => !new[] { "x-cratis-issuer", "x-cratis-subject", "x-cratis-idp", "x-cratis-auth-time", "x-cratis-acr", "x-cratis-amr", "x-cratis-act", "x-cratis-grant-id" }.Contains(h.Name)).ToArray() };
                classified = Set(Set(classified, "x-ms-client-principal", Base64Json("{\"userId\":\"service-7\",\"userRoles\":[],\"claims\":[]}")), "x-ms-client-principal-id", "service-7");
            }
            cases.Add(Signed("credential-kind-" + kind, classified, keys[0]));
            var required = kind is "machine-principal" or "workload"
                ? new[] { "x-cratis-credential-id", "x-cratis-sender-binding" }
                : new[] { "x-cratis-credential-id", "x-cratis-issuer", "x-cratis-subject", "x-cratis-idp", "x-cratis-auth-time", "x-cratis-acr", "x-cratis-sender-binding" };
            if (kind == "on-behalf-session") required = [.. required, "x-cratis-act", "x-cratis-grant-id"];
            foreach (var name in required) SignedChange(kind + "-missing-" + name, classified with { Headers = classified.Headers.Where(h => h.Name != name).ToArray() }, "metadata");
        }
        SignedChange("workload-cannot-fabricate-person-context", Set(input, "x-cratis-credential-kind", "workload"), "metadata");
        const string token = "TEST-ONLY-forwarded-token.not-a-real-credential";
        var bearer = Set(Set(Set(Set(input, "x-cratis-credential-kind", "browser-session"), "x-cratis-credential-id", "session-test"), "x-cratis-sender-binding", "none"), "Authorization", "Bearer " + token);
        var accountKey = new AccountKey(input.RealmIssuer, subject);
        var withHash = Set(bearer, "x-cratis-forwarded-token-sha256", Url(SHA256.HashData(Encoding.UTF8.GetBytes(token)))) with { VerifiedBearer = new("oauth-verifier", accountKey) };
        var matching = Signed("matching-forwarded-bearer", withHash, keys[0]) with { Expected = new("accept", "ok", "oauth-verifier") };
        cases.Add(matching);
        Changed("mismatched-forwarded-bearer", Set(withHash, "Authorization", "Bearer TEST-ONLY-other-token"), "credential", matching);
        SignedChange("forwarded-bearer-without-hash", bearer, "credential");
        SignedChange("hash-without-forwarded-bearer", withHash with { Headers = withHash.Headers.Where(h => !h.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).ToArray() }, "credential");
        Changed("duplicate-authorization-header", withHash with { Headers = [.. withHash.Headers, new("authorization", "Bearer " + token)] }, "credential", matching);
        Changed("non-bearer-authorization", Set(withHash, "Authorization", "Basic TEST-ONLY"), "credential", matching);
        Changed("bearer-token-whitespace-is-not-normalized", Set(withHash, "Authorization", "Bearer " + token + " "), "credential", matching);
        cases.Add(matching with { Name = "bearer-scheme-case-is-not-token-data", Inputs = Set(withHash, "Authorization", "bearer " + token) });
        var oauth = Set(withHash, "x-cratis-credential-kind", "oauth-grant");
        cases.Add(Signed("oauth-bearer-caller-from-oauth-verifier", oauth, keys[0]) with { Expected = new("accept", "ok", "oauth-verifier") });
        SignedChange("bound-bearer-without-verifier-result", oauth with { VerifiedBearer = null }, "credential");
        SignedChange("bound-oauth-bearer-verification-refused", oauth with { VerifiedBearer = new("oauth-verifier", accountKey, Accepted: false) }, "credential");
        var differentSubject = accountKey with { Subject = "091ee384-c4c8-4a09-b278-47aad8b8a8b2" };
        var differentIssuer = accountKey with { Issuer = "https://other.example.test" };
        SignedChange("bound-oauth-bearer-account-subject-mismatch", oauth with { VerifiedBearer = new("oauth-verifier", differentSubject) }, "credential");
        SignedChange("bound-oauth-bearer-account-issuer-mismatch", oauth with { VerifiedBearer = new("oauth-verifier", differentIssuer) }, "credential");
        // Public zero-payload PAT fixture from the Arc PAT proposal, never an issued credential.
        const string patToken = "direct_pat_v1_0000000000000000000000000000000000000000000000000000000000000000_000eb748";
        var pat = Set(Set(Set(withHash, "x-cratis-credential-kind", "pat"), "Authorization", "Bearer " + patToken), "x-cratis-forwarded-token-sha256", Url(SHA256.HashData(Encoding.UTF8.GetBytes(patToken)))) with { VerifiedBearer = new("pat-verifier", accountKey) };
        cases.Add(Signed("pat-bearer-caller-from-local-pat-verifier", pat, keys[0]) with { Expected = new("accept", "ok", "pat-verifier") });
        SignedChange("pat-namespace-bearer-with-browser-session-envelope", Set(pat, "x-cratis-credential-kind", "browser-session"), "credential");
        SignedChange("pat-namespace-bearer-with-oauth-grant-envelope", Set(pat, "x-cratis-credential-kind", "oauth-grant"), "credential");
        var malformedPat = "DIRECT_PAT_unsupported-TEST-ONLY";
        SignedChange("pat-namespace-case-insensitive-before-format", Set(Set(oauth, "Authorization", "Bearer " + malformedPat), "x-cratis-forwarded-token-sha256", Url(SHA256.HashData(Encoding.UTF8.GetBytes(malformedPat)))), "credential");
        SignedChange("pat-envelope-local-verification-refused", pat with { VerifiedBearer = new("pat-verifier", accountKey, Accepted: false) }, "credential");
        SignedChange("pat-envelope-cannot-select-oauth-verifier", pat with { VerifiedBearer = new("oauth-verifier", accountKey) }, "credential");
        SignedChange("bound-pat-account-subject-mismatch", pat with { VerifiedBearer = new("pat-verifier", differentSubject) }, "credential");
        SignedChange("bound-pat-account-issuer-mismatch", pat with { VerifiedBearer = new("pat-verifier", differentIssuer) }, "credential");
        Changed("tamper-x-cratis-forwarded-token-sha256", Set(withHash, "x-cratis-forwarded-token-sha256", Url(new byte[32])), "signature", matching);
        SignedChange("signed-invalid-forwarded-token-hash", Set(withHash, "x-cratis-forwarded-token-sha256", "invalid"), "credential");
        SignedChange("signed-subject-mismatch", Set(input, "x-ms-client-principal-id", "other-user"), "identity");
        SignedChange("signed-malformed-principal", Set(input, "x-ms-client-principal", "not-base64"), "identity");
        SignedChange("signed-null-roles", Set(input, "x-ms-client-principal", Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"userId\":\"user-test\",\"userRoles\":null,\"claims\":[]}"))), "identity");
        var unicodePrincipal = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"userId\":\"user-é\",\"userRoles\":[],\"claims\":[]}"));
        SignedChange("non-uuid-person-subject-rejected", Set(Set(input, "x-ms-client-principal", unicodePrincipal), "x-ms-client-principal-id", "UTF-8''user-%C3%A9"), "identity");
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

    // Producer implementation: LINQ selection and string framing, never called by the consumer.
    static string SignerCanonical(Inputs input, string kid, string stamp, string nonce)
    {
        if (input.Aud.Length is < 1 or > 64 || !AudienceSlug().IsMatch(input.Aud)) throw new Rejected("request");
        if (!UpperMethod().IsMatch(input.Method)) throw new Rejected("request");
        if (!input.Path.StartsWith('/') || input.Path.Any(c => c <= 32 || c >= 127 || c == '#')) throw new Rejected("request");
        var path = input.Path.Split('?')[0];
        if (InvalidEscape().IsMatch(path)) throw new Rejected("request");
        path = PathEscapes().Replace(path, match => match.Value.ToUpperInvariant());
        var exact = new[] { "x-cratis-token-client-id", "x-cratis-token-scope", "x-cratis-tenant-id", "tenant-id", "x-cratis-microservice", "service-id", input.TenantHeader.ToLowerInvariant() };
        var selected = input.Headers.Where(h => !h.Name.Equals("x-cratis-forwarded-identity-signature", StringComparison.OrdinalIgnoreCase))
            .Where(h => h.Name.StartsWith("x-ms-client-principal", StringComparison.OrdinalIgnoreCase) || h.Name.StartsWith("x-cratis-on-behalf", StringComparison.OrdinalIgnoreCase) || exact.Contains(h.Name.ToLowerInvariant()) || ContextHeaders.Contains(h.Name.ToLowerInvariant())).ToArray();
        if (selected.Any(h => !HeaderToken().IsMatch(h.Name))) throw new Rejected("headers");
        var headers = selected.Select(h => new Header(h.Name.ToLowerInvariant(), h.Value.Trim(' ', '\t'))).OrderBy(h => h.Name, StringComparer.Ordinal).ToArray();
        if (headers.Any(h => h.Value.Any(c => c < 32 || c > 126)) || headers.Select(h => h.Name).Distinct(StringComparer.Ordinal).Count() != headers.Length) throw new Rejected("headers");
        if (headers.Length > 64) throw new Rejected("bounds");
        if (RequiredPrincipalHeaders.Any(name => !headers.Any(h => h.Name == name && h.Value.Length > 0))) throw new Rejected("headers");
        var result = new StringBuilder("Cratis.AuthProxy.ForwardedIdentity.v2\n");
        void Frame(string text)
        {
            result.Append(Encoding.UTF8.GetByteCount(text).ToString(CultureInfo.InvariantCulture)).Append(':').Append(text).Append('\n');
            if (Encoding.UTF8.GetByteCount(result.ToString()) > 32768) throw new Rejected("bounds");
        }
        foreach (var text in new[] { input.Aud, kid, stamp, nonce, input.Method, path, headers.Length.ToString(CultureInfo.InvariantCulture) }) Frame(text);
        foreach (var h in headers) { Frame(h.Name); Frame(h.Value); }
        return result.ToString();
    }

    [GeneratedRegex("\\A[a-z][a-z0-9]*(?:-[a-z0-9]+)*\\z", RegexOptions.None, 100)]
    private static partial Regex AudienceSlug();
    [GeneratedRegex("\\A[A-Z0-9!#$%&'*+.^_`|~-]+\\z", RegexOptions.None, 100)]
    private static partial Regex UpperMethod();
    [GeneratedRegex("%(?![0-9A-Fa-f]{2})", RegexOptions.None, 100)]
    private static partial Regex InvalidEscape();
    [GeneratedRegex("\\A[A-Za-z0-9!#$%&'*+.^_`|~-]+\\z", RegexOptions.None, 100)]
    private static partial Regex HeaderToken();

    public static CanonicalCheck CanonicalResult(Inputs input)
    {
        try { return new(SignerCanonical(input, "k1", "1700000000", Nonce), null); }
        catch (Rejected error) { return new(null, error.Message); }
    }

    public static Fixture KeepUnchangedSignatures(Fixture current, Fixture previous)
    {
        var old = previous.Cases.ToDictionary(item => item.Name, StringComparer.Ordinal);
        return current with { Cases = current.Cases.Select(item => old.TryGetValue(item.Name, out var prior) && item.SigningInput == prior.SigningInput && item.Expected == prior.Expected ? item with { Envelope = prior.Envelope } : item).ToList() };
    }

    static string Base64Json(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

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
    readonly Dictionary<(string Aud, string Kid, string Nonce), decimal> _reservations = [];

    public object[] Snapshot() => _reservations.Select(entry => (object)new { entry.Key.Aud, entry.Key.Kid, entry.Key.Nonce, ExpiresAt = entry.Value }).ToArray();

    public bool Reserve(string aud, string kid, string nonce, long timestamp, long now)
    {
        // Models one atomic insert and a shared durable store; not a production store implementation.
        foreach (var key in _reservations.Where(entry => entry.Value <= now).Select(entry => entry.Key).ToArray()) _reservations.Remove(key);
        return _reservations.TryAdd((aud, kid, nonce), (decimal)timestamp + 61);
    }
}

static partial class Reference
{
    [GeneratedRegex("\\A[A-Za-z0-9_-]{1,64}\\z", RegexOptions.None, 100)]
    private static partial Regex KeyIdPattern();

    [GeneratedRegex("\\A[A-Za-z0-9_-]+\\z", RegexOptions.None, 100)]
    private static partial Regex Base64UrlPattern();

    [GeneratedRegex("\\A[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\\z", RegexOptions.None, 100)]
    private static partial Regex SubjectPattern();
    static readonly HashSet<string> ExactHeaders = new(StringComparer.Ordinal)
    {
        "x-cratis-token-client-id", "x-cratis-token-scope", "x-cratis-tenant-id", "tenant-id", "x-cratis-microservice", "service-id",
        "x-cratis-credential-kind", "x-cratis-credential-id", "x-cratis-issuer", "x-cratis-subject", "x-cratis-idp", "x-cratis-auth-time", "x-cratis-acr", "x-cratis-amr", "x-cratis-act", "x-cratis-grant-id", "x-cratis-policy-epoch", "x-cratis-sender-binding", "x-cratis-forwarded-token-sha256"
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
            Metadata(headers, input.RealmIssuer);
            var callerSource = BindForwardedBearer(input, headers);
            if (input.ReplaySensitive)
            {
                if (!input.StoreAvailable || input.StoreFull) throw new Rejected("store");
                if (!store.Reserve(input.Aud, envelope.Kid, envelope.Parts[3], envelope.Timestamp, input.Now)) throw new Rejected("replay");
            }
            return new("accept", "ok", callerSource);
        }
        catch (Rejected error) { return new("reject", error.Message); }
    }

    public static string CanonicalFor(Vector vector) => Encoding.UTF8.GetString(Canonical(vector.Inputs, Parse(vector.Envelope).Parts).Bytes);

    public static CanonicalCheck CanonicalResult(Inputs input)
    {
        try { return new(Encoding.UTF8.GetString(Canonical(input, ["v2", "k1", "1700000000", "AAECAwQFBgcICQoLDA0ODw"]).Bytes), null); }
        catch (Rejected error) { return new(null, error.Message); }
    }

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
        if (input.Aud.Length is < 1 or > 64 || input.Aud[0] is < 'a' or > 'z' || input.Aud.Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')) || input.Aud.Split('-').Any(segment => segment.Length == 0)) throw new Rejected("request");
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
            if (header.Name.Length == 0 || header.Name.Any(c => !(c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9') && !methodToken.Contains(c))) throw new Rejected("headers");
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
        foreach (var value in new[] { input.Aud, parts[1], parts[2], parts[3], input.Method, new string(path), headers.Count.ToString(CultureInfo.InvariantCulture) }) Write(value);
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
            var service = headers.GetValueOrDefault("x-cratis-credential-kind") is "machine-principal" or "workload";
            if (!service && !SubjectPattern().IsMatch(id)) throw new Rejected("identity");
            if (!service && headers.TryGetValue("x-cratis-subject", out var explicitSubject) && explicitSubject.Length > 0 && explicitSubject != id) throw new Rejected("identity");
        }
        catch (Exception error) when (error is FormatException or JsonException or DecoderFallbackException or InvalidOperationException) { throw new Rejected("identity"); }
    }

    static void Metadata(SortedDictionary<string, string> headers, string realmIssuer)
    {
        try
        {
            var kind = Required(headers, "x-cratis-credential-kind");
            if (kind is not ("browser-session" or "oauth-grant" or "pat" or "on-behalf-session" or "machine-principal" or "workload")) throw new Rejected("metadata");
            _ = Required(headers, "x-cratis-credential-id");
            var service = kind is "machine-principal" or "workload";
            if (!service)
            {
                if (Required(headers, "x-cratis-issuer") != realmIssuer) throw new Rejected("metadata");
                _ = Required(headers, "x-cratis-subject");
                _ = Required(headers, "x-cratis-idp");
                if (!NumericDate(Required(headers, "x-cratis-auth-time"))) throw new Rejected("metadata");
                _ = Required(headers, "x-cratis-acr");
            }
            else if (headers.Keys.Any(name => name is "x-cratis-issuer" or "x-cratis-subject" or "x-cratis-idp" or "x-cratis-auth-time" or "x-cratis-acr" or "x-cratis-amr" or "x-cratis-act")) throw new Rejected("metadata");
            foreach (var name in new[] { "x-cratis-idp", "x-cratis-auth-time", "x-cratis-acr", "x-cratis-amr", "x-cratis-act", "x-cratis-grant-id", "x-cratis-policy-epoch" })
            {
                if (headers.TryGetValue(name, out var value) && value.Length == 0) throw new Rejected("metadata");
            }
            if (headers.TryGetValue("x-cratis-policy-epoch", out var epoch) && !Unsigned(epoch, out _)) throw new Rejected("metadata");
            if (headers.TryGetValue("x-cratis-amr", out var methods))
            {
                using var document = DecodeJson(methods);
                if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.EnumerateArray().Any(method => method.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(method.GetString()))) throw new Rejected("metadata");
            }
            if (kind == "on-behalf-session") _ = Required(headers, "x-cratis-act");
            if (headers.TryGetValue("x-cratis-act", out var actor))
            {
                using var document = DecodeJson(actor);
                CheckActor(document.RootElement, 0);
                _ = Required(headers, "x-cratis-grant-id");
            }
            var binding = Required(headers, "x-cratis-sender-binding");
            if (binding != "none")
            {
                using var document = DecodeJson(binding);
                var evidence = document.RootElement;
                if (evidence.ValueKind != JsonValueKind.Object || evidence.EnumerateObject().Count() != 2 || !StringField(evidence, "jkt") || !StringField(evidence, "jti")) throw new Rejected("metadata");
                try { _ = DecodeUrl(evidence.GetProperty("jkt").GetString()!, 32); }
                catch (Rejected) { throw new Rejected("metadata"); }
            }
        }
        catch (Exception error) when (error is FormatException or JsonException or DecoderFallbackException or InvalidOperationException) { throw new Rejected("metadata"); }
    }

    static bool NumericDate(string value) => long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var timestamp) && value == timestamp.ToString(CultureInfo.InvariantCulture);

    [GeneratedRegex("^[a-z0-9-]+_pat_", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 100)]
    private static partial Regex PatNamespace();

    static string? BindForwardedBearer(Inputs input, SortedDictionary<string, string> headers)
    {
        var authorization = input.Headers.Where(header => header.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).ToArray();
        var hasHash = headers.TryGetValue("x-cratis-forwarded-token-sha256", out var hash);
        if (authorization.Length == 0)
        {
            if (hasHash) throw new Rejected("credential");
            return null;
        }
        if (authorization.Length != 1 || !hasHash || !authorization[0].Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) throw new Rejected("credential");
        var token = authorization[0].Value[7..];
        var unpadded = token.TrimEnd('=');
        if (unpadded.Length == 0 || unpadded.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '.' or '_' or '~' or '+' or '/'))) throw new Rejected("credential");
        byte[] expected;
        try { expected = DecodeUrl(hash!, 32); }
        catch (Rejected) { throw new Rejected("credential"); }
        if (!CryptographicOperations.FixedTimeEquals(expected, SHA256.HashData(Encoding.UTF8.GetBytes(token)))) throw new Rejected("credential");
        return VerifiedBearerCallerSource(input, headers, PatNamespace().IsMatch(token));
    }

    static string VerifiedBearerCallerSource(Inputs input, SortedDictionary<string, string> headers, bool patNamespace)
    {
        if (patNamespace && headers["x-cratis-credential-kind"] != "pat") throw new Rejected("credential");
        var callerSource = patNamespace ? "pat-verifier" : "oauth-verifier";
        // Injected result of verifying this exact bearer, not authority derived from signed headers.
        var bearer = input.VerifiedBearer;
        if (bearer?.Accepted != true || bearer.Verifier != callerSource) throw new Rejected("credential");
        // Principal already requires both nonempty signed account-key fields.
        var signedAccountKey = new AccountKey(headers.GetValueOrDefault("x-cratis-issuer", ""), headers.GetValueOrDefault("x-cratis-subject", ""));
        if (signedAccountKey != bearer.AccountKey) throw new Rejected("credential");
        return callerSource;
    }

    static string Required(SortedDictionary<string, string> headers, string name) => headers.TryGetValue(name, out var value) && value.Length > 0 ? value : throw new Rejected("metadata");

    static JsonDocument DecodeJson(string value) => JsonDocument.Parse(new UTF8Encoding(false, true).GetString(Convert.FromBase64String(value)));

    static bool StringField(JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString());

    static void CheckActor(JsonElement actor, int depth)
    {
        if (depth > 8 || actor.ValueKind != JsonValueKind.Object || !StringField(actor, "iss") || !StringField(actor, "sub") || actor.EnumerateObject().Any(field => field.Name is not ("iss" or "sub" or "act"))) throw new Rejected("metadata");
        if (actor.TryGetProperty("act", out var previous)) CheckActor(previous, depth + 1);
    }

    static bool Unsigned(string value, out long number) => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number) && value.Length <= 19 && value == number.ToString(CultureInfo.InvariantCulture);
}

static class IdentityDelivery
{
    public static void Export(Fixture fixture, string directory, JsonSerializerOptions json)
    {
        Directory.CreateDirectory(directory);
        var groups = new Dictionary<string, NonceStore>(StringComparer.Ordinal);
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
            var data = new
            {
                item.Name,
                Description = "Synthetic TEST-ONLY forwarded identity contract case: " + item.Name.Replace('-', ' '),
                Given = new
                {
                    VerificationKeys = fixture.TestKeys.Select(key => new { key.Name, key.KeyIds, key.PublicKeyPem }).ToArray(),
                    item.Inputs,
                    NonceReservations = store.Snapshot()
                },
                When = new { Action = "verify_forwarded_identity_v2", item.Envelope, item.SigningInput },
                Then = new { Outcome = item.Expected.Result, item.Expected.Reason, item.Expected.CallerSource }
            };
            if (Reference.Verify(item, fixture.TestKeys, store) != item.Expected) throw new Rejected("delivery-state");
            File.WriteAllText(Path.Combine(directory, item.Name + ".json"), JsonSerializer.Serialize(data, json) + "\n", new UTF8Encoding(false));
        }
        Console.WriteLine($"Exported {fixture.Cases.Count} Identity data vectors; public verification keys only.");
    }
}
